namespace HelpMuslimWebApp.Domain.Models
{
    public class RecruiterProfile
    {
        public int Id { get; set; }
        public string? UserId { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public bool IsPhoneVisible { get; set; }
        public string? JobTitle { get; set; }
        public string? CompanyName { get; set; }
        public string? CompanySector { get; set; }
        public string? CompanySize { get; set; }
        public string? CompanyWebsite { get; set; }
        public string? CompanyDescription { get; set; }
        public string? City { get; set; }
        public string? LogoPath { get; set; }
        public int OffersCount { get; set; }
        public int OpenOffersCount { get; set; }

        // Entreprise désactivée par l'administration
        public bool IsDisabled { get; set; }
        public DateTime? DisabledAt { get; set; }

        // Compte de test (IdentityServer) : ses offres ne reçoivent que des candidatures de comptes de test
        public bool IsTestAccount { get; set; }

        // Recruteur Pro : offert (administration, « à vie » pour une entreprise fondatrice) et/ou payé (abonnement Stripe)
        public DateTime? ProGrantedUntil { get; set; }
        public DateTime? ProPaidUntil { get; set; }
        public bool IsFounder { get; set; }
        public string? StripeCustomerId { get; set; }
        public string? StripeSubscriptionId { get; set; }
        public bool ProCancelAtPeriodEnd { get; set; }
        public DateTime? LastFreeSponsorAt { get; set; }
        // Alerte quotidienne « nouveaux candidats à 70 % » (activée par défaut)
        public bool MatchAlertsEnabled { get; set; } = true;
        // Email quand un candidat écrit (un par conversation tant que non lue)
        public bool MessageAlertsEnabled { get; set; } = true;
        // Email à chaque nouvelle candidature (activé par défaut)
        public bool ApplicationAlertsEnabled { get; set; } = true;

        // Identité légale (SIRET vérifié ou RNA validé par l'administration)
        public string? Siret { get; set; }
        public string? Siren { get; set; }
        public string? Rna { get; set; }
        public string? LegalName { get; set; }
        public string? LegalAliases { get; set; }
        public DateTime? IdentityVerifiedAt { get; set; }
        public bool IsEmailConfirmed { get; set; }
        public string? SignupIpHash { get; set; }
        public DateTime? FounderSince { get; set; }
        public DateTime? FounderRevokedAt { get; set; }
        public DateTime? ProUntil => Premium.Until(ProGrantedUntil, ProPaidUntil);

        // Offre sponsorisée incluse dans le Pro : une par mois civil
        public bool HasFreeSponsorAvailable =>
            LastFreeSponsorAt == null || LastFreeSponsorAt < new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
