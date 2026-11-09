using System.ComponentModel.DataAnnotations;

namespace HelpMuslimWebApp.Api.Request
{
    public class CandidateProfileRequest
    {
        [Required(ErrorMessage = "Le prénom est obligatoire.")]
        [StringLength(100)]
        public string? FirstName { get; set; }

        [Required(ErrorMessage = "Le nom est obligatoire.")]
        [StringLength(100)]
        public string? LastName { get; set; }

        [StringLength(30)]
        public string? Phone { get; set; }

        // Montrer son email / son numéro aux recruteurs
        public bool IsEmailVisible { get; set; }
        public bool IsPhoneVisible { get; set; }

        public DateOnly? BirthDate { get; set; }

        [StringLength(100)]
        public string? City { get; set; }

        [StringLength(150)]
        public string? Headline { get; set; }

        public string? Situation { get; set; }

        // Disponible à partir de cette date, un an au plus après aujourd'hui (aujourd'hui : tout de suite)
        public DateOnly? AvailableFrom { get; set; }

        [StringLength(150)]
        public string? DesiredJob { get; set; }

        public int? DesiredDomainId { get; set; }
        public int? DesiredProfessionId { get; set; }

        [StringLength(3000)]
        public string? Summary { get; set; }

        [StringLength(255)]
        [Url(ErrorMessage = "L'URL LinkedIn est invalide.")]
        public string? LinkedinUrl { get; set; }

        public List<ExperienceRequest> Experiences { get; set; } = [];
        public List<EducationRequest> Educations { get; set; } = [];
        public List<string> Skills { get; set; } = [];
        public List<LanguageRequest> Languages { get; set; } = [];

        // Ce que je cherche : métiers du domaine recherché (5 max.), contrats, villes (5 max.), télétravail, salaire
        public List<int> WishProfessionIds { get; set; } = [];
        public List<string> WishContractTypes { get; set; } = [];
        public List<string> WishCities { get; set; } = [];
        public bool WishAnywhere { get; set; }
        public bool WishRemote { get; set; }

        // Salaire annuel brut minimum (€) ; null = peu importe
        [Range(0, 500_000, ErrorMessage = "Salaire souhaité invalide.")]
        public int? WishSalaryMin { get; set; }
    }

    public class ExperienceRequest
    {
        [Required(ErrorMessage = "L'intitulé du poste est obligatoire.")]
        [StringLength(150)]
        public string? Title { get; set; }

        [StringLength(150)]
        public string? Company { get; set; }

        [StringLength(100)]
        public string? City { get; set; }

        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public bool? IsCurrent { get; set; }

        [StringLength(2000)]
        public string? Description { get; set; }
    }

    public class EducationRequest
    {
        [Required(ErrorMessage = "L'établissement est obligatoire.")]
        [StringLength(150)]
        public string? School { get; set; }

        [StringLength(150)]
        public string? Degree { get; set; }

        [StringLength(150)]
        public string? FieldOfStudy { get; set; }

        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }

        [StringLength(2000)]
        public string? Description { get; set; }
    }

    public class LanguageRequest
    {
        [Required(ErrorMessage = "La langue est obligatoire.")]
        [StringLength(60)]
        public string? Name { get; set; }

        [StringLength(40)]
        public string? Level { get; set; }
    }
}
