namespace HelpMuslimWebApp.Domain.Models
{
    public class CandidateProfile
    {
        public int Id { get; set; }
        public string? UserId { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public DateOnly? BirthDate { get; set; }
        public string? City { get; set; }
        public string? Headline { get; set; }
        public string? Situation { get; set; }
        // Disponible à partir de cette date (aujourd'hui ou avant : tout de suite) ; vide si non renseigné
        public DateOnly? AvailableFrom { get; set; }
        public string? DesiredJob { get; set; }
        public int? DesiredDomainId { get; set; }
        public string? DesiredDomainName { get; set; }
        public int? DesiredProfessionId { get; set; }
        public string? DesiredProfessionName { get; set; }
        public string? Summary { get; set; }
        public string? PhotoPath { get; set; }
        public string? CvPath { get; set; }
        public string? CvFileName { get; set; }
        public string? LinkedinUrl { get; set; }

        // Partage public de la fiche : jeton du lien (null = désactivé), coordonnées visibles ou non
        public string? ShareToken { get; set; }
        public bool? ShareContact { get; set; }

        // Profil invisible : absent des recherches et suggestions des recruteurs
        public bool? IsHidden { get; set; }
        // Compte de test (IdentityServer) : invisible des recruteurs qui ne sont pas eux-mêmes des comptes de test
        public bool IsTestAccount { get; set; }

        // Coordonnées montrées aux recruteurs
        public bool IsEmailVisible { get; set; }
        public bool IsPhoneVisible { get; set; }
        // CV masqué aux recruteurs sur la fiche
        public bool IsCvHidden { get; set; }

        // Vœux (« Ce que je cherche ») : le domaine est DesiredDomainId, le premier métier est aussi DesiredProfessionId
        public List<string> WishContractTypes { get; set; } = [];
        public List<int> WishProfessionIds { get; set; } = [];
        public List<string> WishCities { get; set; } = [];
        public bool WishAnywhere { get; set; }
        public bool WishRemote { get; set; }
        // Salaire annuel brut minimum (€) ; null = peu importe
        public int? WishSalaryMin { get; set; }

        // Premium : offert (administration) et/ou payé (abonnement Stripe) ; actif jusqu'à la plus tardive des deux dates
        public DateTime? PremiumGrantedUntil { get; set; }
        public DateTime? PremiumPaidUntil { get; set; }
        public string? StripeCustomerId { get; set; }
        public string? StripeSubscriptionId { get; set; }
        public bool PremiumCancelAtPeriodEnd { get; set; }
        public DateTime? PremiumUntil => Premium.Until(PremiumGrantedUntil, PremiumPaidUntil);
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<CandidateExperience> Experiences { get; set; } = [];
        public List<CandidateEducation> Educations { get; set; } = [];
        public List<string> Skills { get; set; } = [];
        public List<CandidateLanguage> Languages { get; set; } = [];
    }
}
