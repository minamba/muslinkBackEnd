using System.Security.Claims;
using HelpMuslimWebApp.Api.Request;
using HelpMuslimWebApp.Api.Services;
using HelpMuslimWebApp.Api.Services.Telegram;
using HelpMuslimWebApp.Api.Utils;
using HelpMuslimWebApp.Api.ViewModels;
using HelpMuslimWebApp.Domain.Models;
using HelpMuslimWebApp.Domain.Services;
using Stripe;
using Stripe.Checkout;

namespace HelpMuslimWebApp.Api.Builders.impl
{
    public class PremiumViewModelBuilder : IPremiumViewModelBuilder
    {
        private readonly IPremiumService _premiumService;
        private readonly ICandidateProfileService _candidateService;
        private readonly IRecruiterProfileService _recruiterService;
        private readonly IJobOfferService _jobOfferService;
        private readonly IProfileResolver _profileResolver;
        private readonly StripeBilling _stripe;
        private readonly TestModeGuard _testMode;
        private readonly IEmailService _emailService;
        private readonly string _frontUrl;
        private readonly ITelegramNotifier _telegram;
        private readonly ILogger<PremiumViewModelBuilder> _logger;

        public PremiumViewModelBuilder(
            IPremiumService premiumService,
            ICandidateProfileService candidateService,
            IRecruiterProfileService recruiterService,
            IJobOfferService jobOfferService,
            IProfileResolver profileResolver,
            StripeBilling stripe,
            TestModeGuard testMode,
            IEmailService emailService,
            IConfiguration config,
            ITelegramNotifier telegram,
            ILogger<PremiumViewModelBuilder> logger)
        {
            _telegram = telegram;
            _testMode = testMode;
            _emailService = emailService;
            _frontUrl = (config["FrontUrl"] ?? "http://localhost:3000").TrimEnd('/');
            _premiumService = premiumService;
            _candidateService = candidateService;
            _recruiterService = recruiterService;
            _jobOfferService = jobOfferService;
            _profileResolver = profileResolver;
            _stripe = stripe;
            _logger = logger;
        }

        public async Task<PremiumInfoViewModel> GetInfoAsync(ClaimsPrincipal user)
        {
            var settings = await _premiumService.GetSettingsAsync();
            // Compte de test en mode test : abonnement sans paiement, même sans Stripe configuré
            var info = new PremiumInfoViewModel { PaymentAvailable = _stripe.IsConfigured || await _testMode.PaysNothingAsync(user) };

            var candidate = user.IsCandidat() ? await _profileResolver.FindCandidateAsync(user) : null;
            // Offre coupée : un abonné garde l'accès à son abonnement (Mon compte : factures, résiliation)
            if (settings.CandidatesEnabled || candidate?.StripeSubscriptionId != null)
            {
                info.Enabled = settings.CandidatesEnabled;
                info.CandidateMonthlyPriceCents = settings.CandidateMonthlyPriceCents;
                if (candidate != null)
                {
                    info.Me = new PremiumStatusViewModel
                    {
                        IsPremium = Premium.IsActive(candidate.PremiumUntil),
                        PremiumUntil = candidate.PremiumUntil,
                        GrantedUntil = Premium.IsActive(candidate.PremiumGrantedUntil) ? candidate.PremiumGrantedUntil : null,
                        IsLifetime = Premium.IsLifetime(candidate.PremiumGrantedUntil),
                        PaidUntil = Premium.IsActive(candidate.PremiumPaidUntil) ? candidate.PremiumPaidUntil : null,
                        HasSubscription = candidate.StripeSubscriptionId != null,
                        CancelAtPeriodEnd = candidate.PremiumCancelAtPeriodEnd,
                        CanManageBilling = _stripe.IsConfigured && !_testMode.IsDemo && candidate.StripeCustomerId != null,
                    };
                }
            }

            var subscriber = user.IsRecruteur() ? await _profileResolver.FindRecruiterAsync(user) : null;
            if (settings.RecruitersEnabled || subscriber?.StripeSubscriptionId != null)
            {
                info.Recruiters = new RecruiterPremiumInfoViewModel
                {
                    Enabled = settings.RecruitersEnabled,
                    MonthlyPriceCents = settings.RecruiterMonthlyPriceCents,
                    YearlyPriceCents = settings.RecruiterYearlyPriceCents,
                    SponsoredOfferPriceCents = settings.SponsoredOfferPriceCents,
                    FreeActiveOffers = Premium.FreeActiveOffers,
                    FreeVisibleMatches = Premium.FreeVisibleMatches,
                    SponsorDays = Premium.SponsorDays,
                };
                if (subscriber is { } recruiter)
                {
                    var isPro = Premium.IsActive(recruiter.ProUntil);
                    info.Recruiters.Me = new RecruiterProStatusViewModel
                    {
                        IsPremium = isPro,
                        PremiumUntil = recruiter.ProUntil,
                        GrantedUntil = Premium.IsActive(recruiter.ProGrantedUntil) ? recruiter.ProGrantedUntil : null,
                        IsLifetime = Premium.IsLifetime(recruiter.ProGrantedUntil),
                        IsFounder = recruiter.IsFounder,
                        PaidUntil = Premium.IsActive(recruiter.ProPaidUntil) ? recruiter.ProPaidUntil : null,
                        HasSubscription = recruiter.StripeSubscriptionId != null,
                        CancelAtPeriodEnd = recruiter.ProCancelAtPeriodEnd,
                        CanManageBilling = _stripe.IsConfigured && !_testMode.IsDemo && recruiter.StripeCustomerId != null,
                        ActiveOffers = await _premiumService.CountActiveOffersAsync(recruiter.Id),
                        FreeSponsorAvailable = isPro && recruiter.HasFreeSponsorAvailable,
                    };
                }
            }
            return info;
        }

        public async Task<RedirectUrlViewModel> CreateCheckoutAsync(ClaimsPrincipal user)
        {
            var settings = await _premiumService.GetSettingsAsync();
            if (!settings.CandidatesEnabled) throw new InvalidOperationException("Muslink Premium n'est pas disponible pour le moment.");

            // Compte de test en mode test, ou démo : un mois de Premium tout de suite, sans paiement
            if (await _testMode.PaysNothingAsync(user))
            {
                var tester = await _profileResolver.GetOrCreateCandidateAsync(user);
                await _premiumService.GrantAsync(tester.Id, tester.PremiumGrantedUntil, 1);
                return new RedirectUrlViewModel { Url = $"{_frontUrl}/tarifs/candidats?paiement=ok" };
            }
            EnsurePayment();

            var candidate = await _profileResolver.GetOrCreateCandidateAsync(user);
            if (candidate.StripeSubscriptionId != null && Premium.IsActive(candidate.PremiumPaidUntil))
                throw new InvalidOperationException("Vous êtes déjà abonné(e) à Muslink Premium.");
            if (string.IsNullOrWhiteSpace(candidate.Email) && candidate.StripeCustomerId == null)
                throw new InvalidOperationException("Ajoutez une adresse email à votre compte avant de vous abonner.");

            return new RedirectUrlViewModel
            {
                Url = await CallStripeAsync(async () => await _stripe.CreateCandidateCheckoutUrlAsync(candidate,
                    await _stripe.EnsurePriceAsync(StripeOfferKind.CandidateMonthly, settings.CandidateMonthlyPriceCents))),
            };
        }

        public async Task<RedirectUrlViewModel> CreateRecruiterCheckoutAsync(ClaimsPrincipal user, string interval)
        {
            var settings = await _premiumService.GetSettingsAsync();
            if (!settings.RecruitersEnabled) throw new InvalidOperationException("Recruteur Pro n'est pas disponible pour le moment.");
            var testing = await _testMode.PaysNothingAsync(user);
            if (!testing) EnsurePayment();

            var recruiter = await _profileResolver.GetOrCreateRecruiterAsync(user);
            if (recruiter.StripeSubscriptionId != null && Premium.IsActive(recruiter.ProPaidUntil))
                throw new InvalidOperationException("Votre entreprise est déjà abonnée à Recruteur Pro.");
            if (Premium.IsLifetime(recruiter.ProGrantedUntil))
                throw new InvalidOperationException("Entreprise fondatrice : Recruteur Pro vous est offert à vie.");

            var yearly = interval == "year";
            // Compte de test en mode test : Recruteur Pro tout de suite (1 mois ou 1 an), sans paiement
            if (testing)
            {
                await _premiumService.GrantRecruiterAsync(recruiter, yearly ? 12 : 1, lifetime: false);
                return new RedirectUrlViewModel { Url = $"{_frontUrl}/tarifs/recruteurs?paiement=ok" };
            }
            return new RedirectUrlViewModel
            {
                Url = await CallStripeAsync(async () => await _stripe.CreateRecruiterCheckoutUrlAsync(recruiter, await _stripe.EnsurePriceAsync(
                    yearly ? StripeOfferKind.RecruiterYearly : StripeOfferKind.RecruiterMonthly,
                    yearly ? settings.RecruiterYearlyPriceCents : settings.RecruiterMonthlyPriceCents))),
            };
        }

        public async Task<RedirectUrlViewModel> CreatePortalAsync(ClaimsPrincipal user)
        {
            if (_testMode.IsDemo) throw new InvalidOperationException("La gestion de l'abonnement n'est pas disponible dans la démonstration.");
            if (user.IsRecruteur())
            {
                var recruiter = await _profileResolver.GetOrCreateRecruiterAsync(user);
                if (recruiter.StripeCustomerId == null) throw new InvalidOperationException("Aucun abonnement à gérer.");
                return new RedirectUrlViewModel { Url = await CallStripeAsync(() => _stripe.CreatePortalUrlAsync(recruiter.StripeCustomerId, "/mon-compte")) };
            }

            var candidate = await _profileResolver.GetOrCreateCandidateAsync(user);
            if (candidate.StripeCustomerId == null) throw new InvalidOperationException("Aucun abonnement à gérer.");
            return new RedirectUrlViewModel { Url = await CallStripeAsync(() => _stripe.CreatePortalUrlAsync(candidate.StripeCustomerId, "/mon-compte")) };
        }

        // Pro : une offre sponsorisée incluse par mois, appliquée tout de suite ; sinon paiement à l'unité
        public async Task<SponsorResultViewModel> SponsorOfferAsync(ClaimsPrincipal user, int jobOfferId)
        {
            var settings = await _premiumService.GetSettingsAsync();
            if (!settings.RecruitersEnabled) throw new InvalidOperationException("Les offres sponsorisées ne sont pas disponibles pour le moment.");

            var recruiter = await _profileResolver.GetOrCreateRecruiterAsync(user);
            var offer = await _jobOfferService.GetJobOfferByIdAsync(jobOfferId) ?? throw new KeyNotFoundException("Offre introuvable.");
            if (offer.RecruiterProfileId != recruiter.Id) throw new UnauthorizedAccessException();
            if (offer.IsOpen != true || offer.IsPubliclyHidden)
                throw new InvalidOperationException("Seule une offre ouverte et visible peut être sponsorisée.");

            if (Premium.IsActive(recruiter.ProUntil) && recruiter.HasFreeSponsorAvailable)
            {
                var until = await _premiumService.SponsorOfferAsync(jobOfferId);
                await _premiumService.MarkFreeSponsorUsedAsync(recruiter.Id);
                return new SponsorResultViewModel { SponsoredUntil = until, Included = true };
            }

            // Compte de test en mode test, ou démo : offre sponsorisée 30 jours tout de suite, sans paiement
            if (await _testMode.PaysNothingAsync(user))
                return new SponsorResultViewModel { SponsoredUntil = await _premiumService.SponsorOfferAsync(jobOfferId), Included = true };

            EnsurePayment();
            var url = await CallStripeAsync(async () => await _stripe.CreateSponsorCheckoutUrlAsync(recruiter, jobOfferId,
                await _stripe.EnsurePriceAsync(StripeOfferKind.SponsoredOffer, settings.SponsoredOfferPriceCents)));
            return new SponsorResultViewModel { Url = url };
        }

        private void EnsurePayment()
        {
            if (!_stripe.IsConfigured) throw new InvalidOperationException("Le paiement en ligne n'est pas encore disponible.");
        }

        // Erreur Stripe (clé invalide, réseau, portail client non activé…) : message clair, détail dans les logs
        private async Task<string> CallStripeAsync(Func<Task<string>> call)
        {
            try
            {
                return await call();
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Appel Stripe en échec.");
                throw new InvalidOperationException("Le service de paiement ne répond pas pour le moment. Réessayez dans quelques instants.");
            }
        }

        // Traité même premium coupé : les abonnements et paiements en cours restent à jour (renouvellement, résiliation)
        public async Task HandleWebhookAsync(string json, string? signature)
        {
            Event stripeEvent;
            try
            {
                stripeEvent = _stripe.ParseEvent(json, signature);
            }
            catch (StripeException)
            {
                throw new ArgumentException("Signature Stripe invalide.");
            }

            switch (stripeEvent.Type)
            {
                case EventTypes.CheckoutSessionCompleted when stripeEvent.Data.Object is Session session:
                    await HandleCheckoutCompletedAsync(session);
                    break;

                case EventTypes.CustomerSubscriptionCreated or EventTypes.CustomerSubscriptionUpdated or EventTypes.CustomerSubscriptionDeleted
                    when stripeEvent.Data.Object is Subscription subscription:
                    await HandleSubscriptionAsync(subscription, stripeEvent.Type == EventTypes.CustomerSubscriptionDeleted);
                    break;
            }
        }

        private async Task HandleCheckoutCompletedAsync(Session session)
        {
            var metadata = session.Metadata ?? [];
            var recruiterId = MetadataId(metadata, StripeBilling.RecruiterMetadataKey);

            if (metadata.TryGetValue(StripeBilling.KindMetadataKey, out var kind) && kind == StripeBilling.SponsorKind
                && session.PaymentStatus == "paid" && MetadataId(metadata, StripeBilling.OfferMetadataKey) is { } offerId)
            {
                var until = await _premiumService.SponsorOfferAsync(offerId);
                _logger.LogInformation("Offre {OfferId} sponsorisée jusqu'au {Until} (paiement Stripe).", offerId, until);
            }

            if (session.CustomerId == null) return;
            if (recruiterId != null)
                await _premiumService.SetRecruiterStripeCustomerAsync(recruiterId.Value, session.CustomerId);
            else if (MetadataId(metadata, StripeBilling.CandidateMetadataKey) is { } candidateId)
                await _premiumService.SetStripeCustomerAsync(candidateId, session.CustomerId);
        }

        private async Task HandleSubscriptionAsync(Subscription subscription, bool deleted)
        {
            var metadata = subscription.Metadata ?? [];
            var recruiterId = MetadataId(metadata, StripeBilling.RecruiterMetadataKey)
                ?? (metadata.ContainsKey(StripeBilling.CandidateMetadataKey) ? null : await _premiumService.FindRecruiterByStripeCustomerAsync(subscription.CustomerId));
            if (recruiterId != null)
            {
                var recruiterState = StripeBilling.ToState(subscription, recruiterId.Value, deleted);
                var recruiterEvents = await _premiumService.ApplyRecruiterSubscriptionAsync(recruiterState);
                await _telegram.SubscriptionEventsAsync(recruiter: true, recruiterId.Value, recruiterEvents, recruiterState);
                _logger.LogInformation("Abonnement Stripe {SubscriptionId} ({Status}) appliqué au recruteur {RecruiterId}.", subscription.Id, subscription.Status, recruiterId);
                return;
            }

            var candidateId = MetadataId(metadata, StripeBilling.CandidateMetadataKey)
                ?? await _premiumService.FindCandidateByStripeCustomerAsync(subscription.CustomerId);
            if (candidateId == null)
            {
                _logger.LogWarning("Abonnement Stripe {SubscriptionId} : compte introuvable.", subscription.Id);
                return;
            }
            var state = StripeBilling.ToState(subscription, candidateId.Value, deleted);
            var events = await _premiumService.ApplySubscriptionAsync(state);
            await _telegram.SubscriptionEventsAsync(recruiter: false, candidateId.Value, events, state);
            _logger.LogInformation("Abonnement Stripe {SubscriptionId} ({Status}) appliqué au candidat {CandidateId}.", subscription.Id, subscription.Status, candidateId);
        }

        private static int? MetadataId(Dictionary<string, string> metadata, string key) =>
            metadata.TryGetValue(key, out var value) && int.TryParse(value, out var id) ? id : null;

        // ---------- Administration ----------

        public Task<PremiumAdminViewModel> GetAdminAsync() => BuildAdminAsync(syncError: null);

        private async Task<PremiumAdminViewModel> BuildAdminAsync(string? syncError)
        {
            var settings = await _premiumService.GetSettingsAsync();
            var offers = new List<StripeOfferStatusViewModel>
            {
                new() { Key = nameof(StripeOfferKind.CandidateMonthly), Label = "Premium candidat (mensuel, TTC)", PriceCents = settings.CandidateMonthlyPriceCents },
                new() { Key = nameof(StripeOfferKind.RecruiterMonthly), Label = "Recruteur Pro (mensuel, HT)", PriceCents = settings.RecruiterMonthlyPriceCents },
                new() { Key = nameof(StripeOfferKind.RecruiterYearly), Label = "Recruteur Pro (annuel, HT)", PriceCents = settings.RecruiterYearlyPriceCents },
                new() { Key = nameof(StripeOfferKind.SponsoredOffer), Label = "Offre sponsorisée 30 jours (HT)", PriceCents = settings.SponsoredOfferPriceCents },
            };
            if (_stripe.IsConfigured)
            {
                try
                {
                    foreach (var offer in offers)
                    {
                        var current = await _stripe.GetOfferAsync(Enum.Parse<StripeOfferKind>(offer.Key));
                        offer.StripePriceCents = current?.AmountCents;
                        offer.StripePriceId = current?.PriceId;
                    }
                }
                catch (StripeException ex)
                {
                    _logger.LogWarning(ex, "Offres Stripe illisibles.");
                    syncError ??= $"Stripe ne répond pas : {ex.StripeError?.Message ?? ex.Message}";
                }
            }

            return new PremiumAdminViewModel
            {
                CandidatesEnabled = settings.CandidatesEnabled,
                CandidateMonthlyPriceCents = settings.CandidateMonthlyPriceCents,
                RecruitersEnabled = settings.RecruitersEnabled,
                RecruiterMonthlyPriceCents = settings.RecruiterMonthlyPriceCents,
                RecruiterYearlyPriceCents = settings.RecruiterYearlyPriceCents,
                SponsoredOfferPriceCents = settings.SponsoredOfferPriceCents,
                UpdatedAt = settings.UpdatedAt,
                StripeConfigured = _stripe.IsConfigured,
                StripeWebhookConfigured = _stripe.IsWebhookConfigured,
                StripeMode = _stripe.Mode,
                StripeOffers = offers,
                RenewingCandidateSubscriptions = (await _premiumService.GetRenewingSubscriptionsAsync(recruiters: false)).Count,
                RenewingRecruiterSubscriptions = (await _premiumService.GetRenewingSubscriptionsAsync(recruiters: true)).Count,
                StripeSyncError = syncError,
                GrantDurations = [.. Premium.GrantDurations],
                Members = (await _premiumService.GetMembersAsync()).Select(m => new PremiumMemberViewModel
                {
                    CandidateProfileId = m.CandidateProfileId,
                    FullName = $"{m.FirstName} {m.LastName}".Trim(),
                    Email = m.Email,
                    PremiumUntil = Premium.Until(m.GrantedUntil, m.PaidUntil),
                    GrantedUntil = Premium.IsActive(m.GrantedUntil) ? m.GrantedUntil : null,
                    PaidUntil = Premium.IsActive(m.PaidUntil) ? m.PaidUntil : null,
                    HasSubscription = m.HasSubscription,
                    CancelAtPeriodEnd = m.CancelAtPeriodEnd,
                }).ToList(),
                ProMembers = (await _premiumService.GetProMembersAsync()).Select(m => new ProMemberViewModel
                {
                    RecruiterProfileId = m.RecruiterProfileId,
                    CompanyName = m.CompanyName,
                    FullName = $"{m.FirstName} {m.LastName}".Trim(),
                    Email = m.Email,
                    ProUntil = Premium.Until(m.GrantedUntil, m.PaidUntil),
                    GrantedUntil = Premium.IsActive(m.GrantedUntil) ? m.GrantedUntil : null,
                    PaidUntil = Premium.IsActive(m.PaidUntil) ? m.PaidUntil : null,
                    IsFounder = m.IsFounder,
                    HasSubscription = m.HasSubscription,
                    CancelAtPeriodEnd = m.CancelAtPeriodEnd,
                }).ToList(),
            };
        }

        // Enregistre les réglages puis crée / met à jour les offres dans Stripe (si Stripe est configuré)
        public async Task<PremiumAdminViewModel> UpdateSettingsAsync(PremiumSettingsRequest model)
        {
            EnsureNotDemo();
            var previous = await _premiumService.GetSettingsAsync();
            var settings = await _premiumService.UpdateSettingsAsync(new PremiumSettings
            {
                CandidatesEnabled = model.CandidatesEnabled,
                CandidateMonthlyPriceCents = model.CandidateMonthlyPriceCents,
                RecruitersEnabled = model.RecruitersEnabled,
                RecruiterMonthlyPriceCents = model.RecruiterMonthlyPriceCents,
                RecruiterYearlyPriceCents = model.RecruiterYearlyPriceCents,
                SponsoredOfferPriceCents = model.SponsoredOfferPriceCents,
            });
            // Offre coupée : ses abonnés ne sont plus prélevés (arrêt en fin de période) et sont prévenus
            var stopped = (0, 0);
            if (previous.CandidatesEnabled && !settings.CandidatesEnabled) stopped = Add(stopped, await StopSubscriptionsAsync(recruiters: false));
            if (previous.RecruitersEnabled && !settings.RecruitersEnabled) stopped = Add(stopped, await StopSubscriptionsAsync(recruiters: true));
            var admin = await BuildAdminAsync(await SyncStripeOffersAsync(settings));
            admin.StoppedSubscriptions = stopped.Item1;
            admin.FailedSubscriptionStops = stopped.Item2;
            return admin;
        }

        private static (int, int) Add((int, int) a, (int, int) b) => (a.Item1 + b.Item1, a.Item2 + b.Item2);

        // Abonnements renouvelés de l'offre coupée : plus de renouvellement chez Stripe, email à l'abonné. (arrêtés, en échec)
        private async Task<(int Stopped, int Failed)> StopSubscriptionsAsync(bool recruiters)
        {
            var subscriptions = await _premiumService.GetRenewingSubscriptionsAsync(recruiters);
            if (subscriptions.Count == 0) return (0, 0);
            if (!_stripe.IsConfigured) return (0, subscriptions.Count);
            var product = recruiters ? "Recruteur Pro" : "Muslink Premium";
            int stopped = 0, failed = 0;
            foreach (var subscription in subscriptions)
            {
                try
                {
                    await _stripe.CancelAtPeriodEndAsync(subscription.SubscriptionId);
                    await _premiumService.MarkCancelAtPeriodEndAsync(recruiters, subscription.ProfileId);
                    stopped++;
                }
                catch (StripeException ex)
                {
                    _logger.LogError(ex, "Abonnement {SubscriptionId} non arrêté dans Stripe.", subscription.SubscriptionId);
                    failed++;
                    continue;
                }
                if (string.IsNullOrWhiteSpace(subscription.Email)) continue;
                try
                {
                    await _emailService.SendSubscriptionStoppedEmailAsync(subscription.Email, subscription.FirstName?.Trim() ?? string.Empty, product, subscription.PaidUntil);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Email d'arrêt d'abonnement non envoyé.");
                }
            }
            _logger.LogInformation("{Product} coupé : {Stopped} abonnement(s) arrêté(s) en fin de période, {Failed} en échec.", product, stopped, failed);
            return (stopped, failed);
        }

        public async Task<PremiumAdminViewModel> SyncStripeAsync()
        {
            EnsureNotDemo();
            return await BuildAdminAsync(await SyncStripeOffersAsync(await _premiumService.GetSettingsAsync()));
        }

        // Démo : offres payantes et catalogue Stripe jamais modifiés
        private void EnsureNotDemo()
        {
            if (_testMode.IsDemo) throw new InvalidOperationException("Les offres payantes ne se règlent pas depuis la démonstration.");
        }

        // Message d'erreur, ou null si les offres sont à jour (ou Stripe non configuré)
        private async Task<string?> SyncStripeOffersAsync(PremiumSettings settings)
        {
            if (!_stripe.IsConfigured) return null;
            try
            {
                await _stripe.EnsurePriceAsync(StripeOfferKind.CandidateMonthly, settings.CandidateMonthlyPriceCents);
                await _stripe.EnsurePriceAsync(StripeOfferKind.RecruiterMonthly, settings.RecruiterMonthlyPriceCents);
                await _stripe.EnsurePriceAsync(StripeOfferKind.RecruiterYearly, settings.RecruiterYearlyPriceCents);
                await _stripe.EnsurePriceAsync(StripeOfferKind.SponsoredOffer, settings.SponsoredOfferPriceCents);
                return null;
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Offres Stripe non synchronisées.");
                return $"Offres non créées dans Stripe : {ex.StripeError?.Message ?? ex.Message}";
            }
        }

        public async Task<bool> GrantAsync(int candidateProfileId, PremiumGrantRequest model)
        {
            if (model.Months == null && !model.Lifetime) throw new ArgumentException("Choisissez une durée.");
            var candidate = await _candidateService.GetCandidateProfileByIdAsync(candidateProfileId);
            return candidate != null && await _premiumService.GrantAsync(candidateProfileId, candidate.PremiumGrantedUntil, model.Months, model.Lifetime);
        }

        public async Task<bool> RevokeGrantAsync(int candidateProfileId) =>
            await _candidateService.GetCandidateProfileByIdAsync(candidateProfileId) != null
            && await _premiumService.GrantAsync(candidateProfileId, null, null);

        public async Task<bool> GrantRecruiterAsync(int recruiterProfileId, PremiumGrantRequest model)
        {
            if (model.Months == null && !model.Lifetime) throw new ArgumentException("Choisissez une durée.");
            var recruiter = await _recruiterService.GetRecruiterProfileByIdAsync(recruiterProfileId);
            return recruiter != null && await _premiumService.GrantRecruiterAsync(recruiter, model.Months, model.Lifetime);
        }

        public async Task<bool> RevokeRecruiterGrantAsync(int recruiterProfileId)
        {
            var recruiter = await _recruiterService.GetRecruiterProfileByIdAsync(recruiterProfileId);
            return recruiter != null && await _premiumService.GrantRecruiterAsync(recruiter, null, false);
        }

        public async Task<bool> StopSubscriptionAsync(bool recruiter, int profileId)
        {
            string? subscriptionId, email, firstName;
            DateTime? paidUntil;
            if (recruiter)
            {
                if (await _recruiterService.GetRecruiterProfileByIdAsync(profileId) is not { } profile) return false;
                (subscriptionId, email, firstName, paidUntil) = (profile.StripeSubscriptionId, profile.Email, profile.FirstName, profile.ProPaidUntil);
                if (profile.ProCancelAtPeriodEnd) throw new InvalidOperationException("Cet abonnement est déjà arrêté : il prend fin à la date de fin de période.");
            }
            else
            {
                if (await _candidateService.GetCandidateProfileByIdAsync(profileId) is not { } profile) return false;
                (subscriptionId, email, firstName, paidUntil) = (profile.StripeSubscriptionId, profile.Email, profile.FirstName, profile.PremiumPaidUntil);
                if (profile.PremiumCancelAtPeriodEnd) throw new InvalidOperationException("Cet abonnement est déjà arrêté : il prend fin à la date de fin de période.");
            }
            if (subscriptionId == null || !Premium.IsActive(paidUntil)) throw new InvalidOperationException("Aucun abonnement payé en cours.");
            if (!_stripe.IsConfigured) throw new InvalidOperationException("Stripe n'est pas configuré : arrêtez l'abonnement depuis le tableau de bord Stripe.");

            try
            {
                await _stripe.CancelAtPeriodEndAsync(subscriptionId);
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Abonnement {SubscriptionId} non arrêté dans Stripe (administration).", subscriptionId);
                throw new InvalidOperationException("Stripe n'a pas pu arrêter l'abonnement : réessayez, ou faites-le depuis le tableau de bord Stripe.");
            }
            await _premiumService.MarkCancelAtPeriodEndAsync(recruiter, profileId);

            if (!string.IsNullOrWhiteSpace(email))
            {
                try
                {
                    await _emailService.SendSubscriptionStoppedEmailAsync(email, firstName?.Trim() ?? string.Empty,
                        recruiter ? "Recruteur Pro" : "Muslink Premium", paidUntil, offerStopped: false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Email d'arrêt d'abonnement non envoyé.");
                }
            }
            return true;
        }

        public async Task<RecruiterGrantPreviewViewModel> PreviewRecruitersGrantAsync(bool includeWithoutOffers) =>
            new() { Count = await _premiumService.CountRecruitersForGrantAsync(includeWithoutOffers) };

        public async Task<RecruiterGrantPreviewViewModel> GrantRecruitersAsync(RecruiterBulkGrantRequest model) =>
            new() { Count = await _premiumService.GrantRecruitersAsync(model.Months, model.Lifetime, model.IncludeWithoutOffers) };
    }
}
