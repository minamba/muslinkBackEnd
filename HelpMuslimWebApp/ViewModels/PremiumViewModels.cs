namespace HelpMuslimWebApp.Api.ViewModels
{
    // GET /premium : premium candidats et Recruteur Pro (publics) et, pour le compte connecté, son abonnement.
    // Public coupé : seul Enabled = false est renseigné pour ce public (pas de page Tarifs, pas d'avantages ni de limites).
    public class PremiumInfoViewModel
    {
        // Premium candidats
        public bool Enabled { get; set; }
        public int CandidateMonthlyPriceCents { get; set; }
        public PremiumStatusViewModel? Me { get; set; }

        // Paiement en ligne disponible (Stripe configuré)
        public bool PaymentAvailable { get; set; }

        public RecruiterPremiumInfoViewModel Recruiters { get; set; } = new();
    }

    public class PremiumStatusViewModel
    {
        public bool IsPremium { get; set; }
        public DateTime? PremiumUntil { get; set; }
        // Offert depuis l'administration (31/12/9999 : à vie) / payé (abonnement)
        public DateTime? GrantedUntil { get; set; }
        public bool IsLifetime { get; set; }
        public DateTime? PaidUntil { get; set; }
        public bool HasSubscription { get; set; }
        // Résiliation demandée : l'abonnement s'arrête à la fin de la période payée
        public bool CancelAtPeriodEnd { get; set; }
        // Espace client Stripe accessible (gérer, résilier, factures)
        public bool CanManageBilling { get; set; }
    }

    public class RecruiterPremiumInfoViewModel
    {
        public bool Enabled { get; set; }
        // Prix HT en centimes
        public int MonthlyPriceCents { get; set; }
        public int YearlyPriceCents { get; set; }
        public int SponsoredOfferPriceCents { get; set; }
        public int FreeActiveOffers { get; set; }
        public int FreeVisibleMatches { get; set; }
        public int SponsorDays { get; set; }
        public RecruiterProStatusViewModel? Me { get; set; }
    }

    public class RecruiterProStatusViewModel : PremiumStatusViewModel
    {
        public bool IsFounder { get; set; }
        // Offres actives hors bénévolat (limite de l'offre gratuite)
        public int ActiveOffers { get; set; }
        // Offre sponsorisée incluse ce mois-ci encore disponible
        public bool FreeSponsorAvailable { get; set; }
    }

    // POST /premium/offers/{id}/sponsor : parrainage inclus dans le Pro (appliqué tout de suite) ou page de paiement
    public class SponsorResultViewModel
    {
        public string? Url { get; set; }
        public DateTime? SponsoredUntil { get; set; }
        public bool Included { get; set; }
    }

    // Administration › Premium
    public class PremiumAdminViewModel
    {
        public bool CandidatesEnabled { get; set; }
        public int CandidateMonthlyPriceCents { get; set; }
        public bool RecruitersEnabled { get; set; }
        public int RecruiterMonthlyPriceCents { get; set; }
        public int RecruiterYearlyPriceCents { get; set; }
        public int SponsoredOfferPriceCents { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public bool StripeConfigured { get; set; }
        public bool StripeWebhookConfigured { get; set; }
        // « test » ou « live »
        public string? StripeMode { get; set; }
        // Offres dans Stripe : prix réglé ici et prix en place dans Stripe (null : pas encore créée)
        public List<StripeOfferStatusViewModel> StripeOffers { get; set; } = [];
        public string? StripeSyncError { get; set; }

        public List<int> GrantDurations { get; set; } = [];
        public List<PremiumMemberViewModel> Members { get; set; } = [];
        public List<ProMemberViewModel> ProMembers { get; set; } = [];

        // Abonnements payés en cours, renouvelés : arrêtés en fin de période si l'offre est coupée
        public int RenewingCandidateSubscriptions { get; set; }
        public int RenewingRecruiterSubscriptions { get; set; }
        // Offre tout juste coupée : abonnements arrêtés, et ceux que Stripe a refusé d'arrêter (à faire dans Stripe)
        public int StoppedSubscriptions { get; set; }
        public int FailedSubscriptionStops { get; set; }
    }

    public class StripeOfferStatusViewModel
    {
        public string Key { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public int PriceCents { get; set; }
        public int? StripePriceCents { get; set; }
        public string? StripePriceId { get; set; }
    }

    public class PremiumMemberViewModel
    {
        public int CandidateProfileId { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public DateTime? PremiumUntil { get; set; }
        public DateTime? GrantedUntil { get; set; }
        public DateTime? PaidUntil { get; set; }
        public bool HasSubscription { get; set; }
        public bool CancelAtPeriodEnd { get; set; }
    }

    public class ProMemberViewModel
    {
        public int RecruiterProfileId { get; set; }
        public string? CompanyName { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public DateTime? ProUntil { get; set; }
        public DateTime? GrantedUntil { get; set; }
        public DateTime? PaidUntil { get; set; }
        public bool IsFounder { get; set; }
        public bool HasSubscription { get; set; }
        public bool CancelAtPeriodEnd { get; set; }
    }

    // Offre groupée du Pro : nombre de recruteurs concernés
    public class RecruiterGrantPreviewViewModel
    {
        public int Count { get; set; }
    }

    public class RedirectUrlViewModel
    {
        public string Url { get; set; } = string.Empty;
    }
}
