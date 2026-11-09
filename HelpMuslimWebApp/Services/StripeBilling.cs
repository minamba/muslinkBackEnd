using HelpMuslimWebApp.Domain.Models;
using Stripe;
using Stripe.Checkout;

namespace HelpMuslimWebApp.Api.Services
{
    // Offres payantes de Muslink dans Stripe
    public enum StripeOfferKind { CandidateMonthly, RecruiterMonthly, RecruiterYearly, SponsoredOffer }

    // Paiements via Stripe : pages de paiement (Checkout), espace client (résiliation en quelques clics, moyen de paiement,
    // factures) et événements reçus par webhook.
    // Réglages : Stripe:SecretKey (sk_test_… en développement, sk_live_… en production) et Stripe:WebhookSecret (whsec_…).
    // Les prix viennent de l'administration : produits et prix sont créés dans Stripe par l'Api (rien à créer à la main).
    public class StripeBilling
    {
        public const string CandidateMetadataKey = "candidateProfileId";
        public const string RecruiterMetadataKey = "recruiterProfileId";
        public const string KindMetadataKey = "kind";
        public const string SponsorKind = "sponsor";
        public const string OfferMetadataKey = "jobOfferId";

        // Produit, nom et périodicité de chaque offre ; la clé (lookup key) désigne le prix en cours
        private record OfferDefinition(string ProductId, string ProductName, string Description, string LookupKey, string? Interval, string Label);

        private static readonly Dictionary<StripeOfferKind, OfferDefinition> Offers = new()
        {
            [StripeOfferKind.CandidateMonthly] = new("muslink_premium_candidat", "Muslink Premium",
                "Alertes email instantanées et mise en avant auprès des recruteurs", "muslink_premium_candidat_mensuel", "month", "Premium candidat"),
            [StripeOfferKind.RecruiterMonthly] = new("muslink_recruteur_pro", "Muslink Recruteur Pro",
                "Offres illimitées, contact de tous les candidats, une offre sponsorisée par mois", "muslink_recruteur_pro_mensuel", "month", "Recruteur Pro mensuel"),
            [StripeOfferKind.RecruiterYearly] = new("muslink_recruteur_pro", "Muslink Recruteur Pro",
                "Offres illimitées, contact de tous les candidats, une offre sponsorisée par mois", "muslink_recruteur_pro_annuel", "year", "Recruteur Pro annuel"),
            [StripeOfferKind.SponsoredOffer] = new("muslink_offre_sponsorisee", "Offre sponsorisée Muslink",
                "Votre offre en tête des listes pendant 30 jours, avec un badge « Sponsorisée »", "muslink_offre_sponsorisee_30j", null, "Offre sponsorisée 30 jours"),
        };

        // Marge après la fin de période : le renouvellement (et son webhook) peut arriver quelques heures plus tard
        private static readonly TimeSpan RenewalGrace = TimeSpan.FromDays(2);
        private static readonly HashSet<string> ActiveStatuses = ["active", "trialing", "past_due"];

        private readonly string? _secretKey;
        private readonly string? _webhookSecret;
        private readonly string _frontUrl;

        public StripeBilling(IConfiguration config)
        {
            _secretKey = config["Stripe:SecretKey"]?.Trim();
            _webhookSecret = config["Stripe:WebhookSecret"]?.Trim();
            _frontUrl = (config["FrontUrl"] ?? "http://localhost:3000").TrimEnd('/');
        }

        public bool IsConfigured => !string.IsNullOrWhiteSpace(_secretKey);
        public bool IsWebhookConfigured => !string.IsNullOrWhiteSpace(_webhookSecret);
        // « test » (fausses cartes) ou « live » (vrais paiements), d'après le préfixe de la clé
        public string? Mode => !IsConfigured ? null : _secretKey!.Contains("_live_") ? "live" : "test";

        // Prix en cours d'une offre dans cet environnement Stripe (null : pas encore créée)
        public async Task<StripeOffer?> GetOfferAsync(StripeOfferKind kind)
        {
            var prices = await new PriceService(Client()).ListAsync(new PriceListOptions { LookupKeys = [Offers[kind].LookupKey], Active = true, Limit = 1 });
            var price = prices.Data.FirstOrDefault();
            return price == null ? null : new StripeOffer(price.Id, (int)(price.UnitAmount ?? 0));
        }

        // Produit et prix créés si besoin, dans l'environnement de la clé (test ou production).
        // Nouveau montant : nouveau prix, l'ancien est archivé (les abonnés en cours le gardent).
        public async Task<string> EnsurePriceAsync(StripeOfferKind kind, int amountCents)
        {
            var current = await GetOfferAsync(kind);
            if (current != null && current.AmountCents == amountCents) return current.PriceId;

            var definition = Offers[kind];
            var client = Client();
            await EnsureProductAsync(client, definition);
            var prices = new PriceService(client);
            var price = await prices.CreateAsync(new PriceCreateOptions
            {
                Product = definition.ProductId,
                Currency = "eur",
                UnitAmount = amountCents,
                Recurring = definition.Interval == null ? null : new PriceRecurringOptions { Interval = definition.Interval },
                LookupKey = definition.LookupKey,
                TransferLookupKey = true,
                Nickname = $"{definition.Label} — {amountCents / 100m:0.00} €",
            });
            if (current != null) await prices.UpdateAsync(current.PriceId, new PriceUpdateOptions { Active = false });
            return price.Id;
        }

        private static async Task EnsureProductAsync(StripeClient client, OfferDefinition definition)
        {
            var products = new ProductService(client);
            try
            {
                var product = await products.GetAsync(definition.ProductId);
                if (!product.Active) await products.UpdateAsync(definition.ProductId, new ProductUpdateOptions { Active = true });
            }
            catch (StripeException ex) when (ex.StripeError?.Code == "resource_missing")
            {
                await products.CreateAsync(new ProductCreateOptions { Id = definition.ProductId, Name = definition.ProductName, Description = definition.Description });
            }
        }

        public async Task<string> CreateCandidateCheckoutUrlAsync(CandidateProfile candidate, string priceId)
        {
            var metadata = new Dictionary<string, string> { [CandidateMetadataKey] = candidate.Id.ToString() };
            var options = new SessionCreateOptions
            {
                Mode = "subscription",
                ClientReferenceId = candidate.Id.ToString(),
                Customer = candidate.StripeCustomerId,
                CustomerEmail = candidate.StripeCustomerId == null ? candidate.Email : null,
                Locale = "fr",
                AllowPromotionCodes = true,
                LineItems = [new SessionLineItemOptions { Price = priceId, Quantity = 1 }],
                SubscriptionData = new SessionSubscriptionDataOptions { Metadata = metadata },
                Metadata = metadata,
                SuccessUrl = $"{_frontUrl}/tarifs/candidats?paiement=ok",
                CancelUrl = $"{_frontUrl}/tarifs/candidats?paiement=annule",
            };
            return (await new SessionService(Client()).CreateAsync(options)).Url;
        }

        // Entreprises : adresse de facturation et numéro de TVA intracommunautaire demandés (factures en règle)
        public async Task<string> CreateRecruiterCheckoutUrlAsync(RecruiterProfile recruiter, string priceId)
        {
            var metadata = new Dictionary<string, string> { [RecruiterMetadataKey] = recruiter.Id.ToString() };
            var options = BusinessCheckout(recruiter, "subscription");
            options.LineItems = [new SessionLineItemOptions { Price = priceId, Quantity = 1 }];
            options.SubscriptionData = new SessionSubscriptionDataOptions { Metadata = metadata };
            options.Metadata = metadata;
            options.SuccessUrl = $"{_frontUrl}/tarifs/recruteurs?paiement=ok";
            options.CancelUrl = $"{_frontUrl}/tarifs/recruteurs?paiement=annule";
            return (await new SessionService(Client()).CreateAsync(options)).Url;
        }

        // Offre sponsorisée à l'unité : paiement unique avec facture
        public async Task<string> CreateSponsorCheckoutUrlAsync(RecruiterProfile recruiter, int jobOfferId, string priceId)
        {
            var options = BusinessCheckout(recruiter, "payment");
            options.LineItems = [new SessionLineItemOptions { Price = priceId, Quantity = 1 }];
            options.InvoiceCreation = new SessionInvoiceCreationOptions { Enabled = true };
            options.CustomerCreation = recruiter.StripeCustomerId == null ? "always" : null;
            options.Metadata = new Dictionary<string, string>
            {
                [KindMetadataKey] = SponsorKind,
                [RecruiterMetadataKey] = recruiter.Id.ToString(),
                [OfferMetadataKey] = jobOfferId.ToString(),
            };
            options.SuccessUrl = $"{_frontUrl}/recruteur/offres?sponsor=ok";
            options.CancelUrl = $"{_frontUrl}/recruteur/offres?sponsor=annule";
            return (await new SessionService(Client()).CreateAsync(options)).Url;
        }

        private static SessionCreateOptions BusinessCheckout(RecruiterProfile recruiter, string mode) => new()
        {
            Mode = mode,
            ClientReferenceId = recruiter.Id.ToString(),
            Customer = recruiter.StripeCustomerId,
            CustomerEmail = recruiter.StripeCustomerId == null ? recruiter.Email : null,
            // Client existant : Stripe met à jour son nom et son adresse avec ceux saisis
            CustomerUpdate = recruiter.StripeCustomerId == null ? null : new SessionCustomerUpdateOptions { Name = "auto", Address = "auto" },
            BillingAddressCollection = "required",
            TaxIdCollection = new SessionTaxIdCollectionOptions { Enabled = true },
            Locale = "fr",
            AllowPromotionCodes = true,
        };

        // Abonnement arrêté à la fin de la période déjà payée : plus aucun prélèvement, avantages gardés jusque-là
        public Task CancelAtPeriodEndAsync(string subscriptionId) =>
            new SubscriptionService(Client()).UpdateAsync(subscriptionId, new SubscriptionUpdateOptions { CancelAtPeriodEnd = true });

        // Compte supprimé : abonnement arrêté tout de suite (rien n'est remboursé, plus rien n'est prélevé).
        // Déjà terminé ou inconnu de Stripe : rien à faire
        public async Task CancelNowAsync(string subscriptionId)
        {
            var service = new SubscriptionService(Client());
            try
            {
                var subscription = await service.GetAsync(subscriptionId);
                if (subscription.Status is "canceled" or "incomplete_expired") return;
                await service.CancelAsync(subscriptionId);
            }
            catch (StripeException ex) when (ex.StripeError?.Code == "resource_missing")
            {
            }
        }

        public async Task<string> CreatePortalUrlAsync(string customerId, string returnPath)
        {
            var session = await new Stripe.BillingPortal.SessionService(Client()).CreateAsync(new Stripe.BillingPortal.SessionCreateOptions
            {
                Customer = customerId,
                ReturnUrl = $"{_frontUrl}{returnPath}",
                Locale = "fr",
            });
            return session.Url;
        }

        // Événement signé par Stripe ; une signature invalide lève une StripeException
        public Event ParseEvent(string json, string? signature)
        {
            if (!IsWebhookConfigured) throw new InvalidOperationException("Webhook Stripe non configuré (Stripe:WebhookSecret).");
            return EventUtility.ConstructEvent(json, signature ?? string.Empty, _webhookSecret!, throwOnApiVersionMismatch: false);
        }

        // Abonnement Stripe → état : payé jusqu'à la fin de la période en cours (+ marge), ou terminé.
        // Résiliation programmée : cancel_at_period_end, ou date de fin cancel_at (portail client des versions récentes de l'API) ;
        // les avantages s'arrêtent alors à cette date exacte, sans marge.
        public static PremiumSubscriptionState ToState(Subscription subscription, int profileId, bool deleted)
        {
            var periodEnd = subscription.Items?.Data?.Select(i => i.CurrentPeriodEnd).DefaultIfEmpty().Max();
            var active = !deleted && ActiveStatuses.Contains(subscription.Status) && periodEnd > DateTime.UnixEpoch;
            DateTime? endsAt = subscription.CancelAt is { } cancelAt && cancelAt > DateTime.UnixEpoch ? cancelAt
                : subscription.CancelAtPeriodEnd ? periodEnd : null;
            return new PremiumSubscriptionState
            {
                ProfileId = profileId,
                CustomerId = subscription.CustomerId,
                SubscriptionId = subscription.Id,
                PaidUntil = !active ? null : endsAt ?? periodEnd!.Value + RenewalGrace,
                CancelAtPeriodEnd = endsAt != null,
                Plan = subscription.Items?.Data?.FirstOrDefault()?.Price?.Recurring?.Interval,
            };
        }

        private StripeClient Client() =>
            IsConfigured ? new StripeClient(_secretKey!)
                : throw new InvalidOperationException("Le paiement en ligne n'est pas encore configuré.");
    }

    // Prix Stripe en cours d'une offre (centimes)
    public record StripeOffer(string PriceId, int AmountCents);
}
