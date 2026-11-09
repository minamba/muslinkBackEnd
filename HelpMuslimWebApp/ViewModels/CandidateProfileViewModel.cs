namespace HelpMuslimWebApp.Api.ViewModels
{
    public class CandidateProfileViewModel
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
        public string? PhotoUrl { get; set; }
        public bool HasCv { get; set; }
        public string? CvFileName { get; set; }
        public string? LinkedinUrl { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<ExperienceViewModel> Experiences { get; set; } = [];
        public List<EducationViewModel> Educations { get; set; } = [];
        public List<string> Skills { get; set; } = [];
        public List<LanguageViewModel> Languages { get; set; } = [];

        // Renseigné pour un recruteur : candidat dans ses favoris
        public bool IsFavorite { get; set; }

        // Profil rendu invisible par le candidat
        public bool IsHidden { get; set; }

        // Coordonnées montrées aux recruteurs (sinon Email et Phone sont vides pour eux)
        public bool IsEmailVisible { get; set; }
        public bool IsPhoneVisible { get; set; }
        // CV masqué aux recruteurs (le candidat et l'administration le voient toujours)
        public bool IsCvHidden { get; set; }

        // Ce que je cherche : réservé au candidat lui-même et à l'administration (vides pour les recruteurs)
        public List<int> WishProfessionIds { get; set; } = [];
        public List<string> WishContractTypes { get; set; } = [];
        public List<string> WishCities { get; set; } = [];
        public bool WishAnywhere { get; set; }
        public bool WishRemote { get; set; }
        public int? WishSalaryMin { get; set; }

        // Premium (candidat lui-même et administration ; vides pour les recruteurs)
        public bool IsPremium { get; set; }
        public DateTime? PremiumGrantedUntil { get; set; }
        public DateTime? PremiumPaidUntil { get; set; }
        public bool HasPremiumSubscription { get; set; }
        // Abonnement payé qui s'arrête à la fin de la période (résiliation demandée)
        public bool PremiumCancelAtPeriodEnd { get; set; }
    }

    public class ExperienceViewModel
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? Company { get; set; }
        public string? City { get; set; }
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public bool? IsCurrent { get; set; }
        public string? Description { get; set; }
    }

    public class EducationViewModel
    {
        public int Id { get; set; }
        public string? School { get; set; }
        public string? Degree { get; set; }
        public string? FieldOfStudy { get; set; }
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public string? Description { get; set; }
    }

    public class LanguageViewModel
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Level { get; set; }
    }
}
