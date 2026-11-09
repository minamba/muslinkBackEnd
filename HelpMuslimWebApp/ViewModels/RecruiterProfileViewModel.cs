namespace HelpMuslimWebApp.Api.ViewModels
{
    public class RecruiterProfileViewModel
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
        public string? LogoUrl { get; set; }
        public int OffersCount { get; set; }
        public int OpenOffersCount { get; set; }

        // Entreprise désactivée par l'administration (bandeau et badge côté recruteur)
        public bool IsDisabled { get; set; }

        // Entreprise fondatrice (Pro offert à vie) : badge
        public bool IsFounder { get; set; }

        // Recruteur Pro : recruteur lui-même et administration (vides sur la fiche publique)
        public bool IsPro { get; set; }
        public DateTime? ProGrantedUntil { get; set; }
        public DateTime? ProPaidUntil { get; set; }
        public bool HasProSubscription { get; set; }
        public bool ProCancelAtPeriodEnd { get; set; }
        // Alerte quotidienne des nouveaux candidats à 70 % (recruteur lui-même)
        public bool MatchAlertsEnabled { get; set; }
        public bool MessageAlertsEnabled { get; set; }
        public bool ApplicationAlertsEnabled { get; set; }

        // Identité légale (recruteur lui-même et administration) : SIRET vérifié ou RNA validé
        public string? Siret { get; set; }
        public string? Rna { get; set; }
        public string? LegalName { get; set; }
        public bool IdentityVerified { get; set; }
        // Sa propre fiche : compte de test et mode test activé, la vérification de l'entreprise n'est pas exigée
        public bool VerificationExempt { get; set; }
        public DateTime? FounderSince { get; set; }
        public DateTime? DisabledAt { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
