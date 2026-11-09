namespace HelpMuslimWebApp.Dal.Entities
{
    public partial class CandidateProfile
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
        public int? DesiredProfessionId { get; set; }
        public string? Summary { get; set; }
        public string? PhotoPath { get; set; }
        public string? CvPath { get; set; }
        public string? CvFileName { get; set; }
        public string? LinkedinUrl { get; set; }
        public string? ShareToken { get; set; }
        public bool? ShareContact { get; set; }
        public bool? IsHidden { get; set; }
        // Compte de test (IdentityServer) : invisible des recruteurs qui ne sont pas eux-mêmes des comptes de test
        public bool IsTestAccount { get; set; }

        // Coordonnées montrées aux recruteurs (masquées par défaut ; la messagerie reste toujours possible)
        public bool? IsEmailVisible { get; set; }
        public bool? IsPhoneVisible { get; set; }
        // CV masqué aux recruteurs sur la fiche (il reste joignable à une candidature ou envoyé dans la messagerie)
        public bool? IsCvHidden { get; set; }

        // Vœux (« Ce que je cherche ») : contrats, métiers du domaine recherché, villes, télétravail, salaire annuel brut minimum
        public string[]? WishContractTypes { get; set; }
        public int[]? WishProfessionIds { get; set; }
        public string[]? WishCities { get; set; }
        public bool? WishAnywhere { get; set; }
        public bool? WishRemote { get; set; }
        public int? WishSalaryMin { get; set; }

        // Premium : offert depuis l'administration jusqu'à…, payé (abonnement Stripe) jusqu'à…
        public DateTime? PremiumGrantedUntil { get; set; }
        public DateTime? PremiumPaidUntil { get; set; }
        public string? StripeCustomerId { get; set; }
        public string? StripeSubscriptionId { get; set; }
        // Résiliation demandée : l'abonnement s'arrête à la fin de la période payée
        public bool? PremiumCancelAtPeriodEnd { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public virtual JobDomain? DesiredDomain { get; set; }
        public virtual Profession? DesiredProfession { get; set; }
        public virtual ICollection<CandidateExperience> Experiences { get; set; } = new List<CandidateExperience>();
        public virtual ICollection<CandidateEducation> Educations { get; set; } = new List<CandidateEducation>();
        public virtual ICollection<CandidateSkill> Skills { get; set; } = new List<CandidateSkill>();
        public virtual ICollection<CandidateLanguage> Languages { get; set; } = new List<CandidateLanguage>();
        public virtual ICollection<JobApplication> Applications { get; set; } = new List<JobApplication>();
    }
}
