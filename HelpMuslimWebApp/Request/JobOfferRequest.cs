using System.ComponentModel.DataAnnotations;

namespace HelpMuslimWebApp.Api.Request
{
    public class JobOfferRequest
    {
        public int Id { get; set; }   // inclure Id pour les PUT

        [Required(ErrorMessage = "Le titre est obligatoire.")]
        [StringLength(150, MinimumLength = 5, ErrorMessage = "Le titre doit contenir entre 5 et 150 caractères.")]
        public string? Title { get; set; }

        [Required(ErrorMessage = "La description est obligatoire.")]
        [StringLength(8000, MinimumLength = 30, ErrorMessage = "La description doit contenir au moins 30 caractères.")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Le domaine est obligatoire.")]
        public int? DomainId { get; set; }

        public int? ProfessionId { get; set; }

        [Required(ErrorMessage = "Le type de contrat est obligatoire.")]
        public string? ContractType { get; set; }

        [StringLength(100)]
        public string? City { get; set; }

        public bool? IsRemote { get; set; }

        // Années d'expérience demandées dans le domaine (0 : débutant accepté) ; vide : non précisé
        [Range(0, 30, ErrorMessage = "L'expérience demandée doit être comprise entre 0 et 30 ans.")]
        public int? ExperienceYears { get; set; }

        [StringLength(100)]
        public string? Salary { get; set; }

        [StringLength(100)]
        public string? Duration { get; set; }

        public DateOnly? StartDate { get; set; }

        // Compétences attendues (correspondance avec les candidats)
        [MaxLength(15, ErrorMessage = "15 compétences requises au maximum.")]
        public List<string> RequiredSkills { get; set; } = [];

        [MaxLength(15, ErrorMessage = "15 compétences bonus au maximum.")]
        public List<string> BonusSkills { get; set; } = [];

        // Admin uniquement : recruteur pour le compte duquel l'offre est créée
        public int? RecruiterProfileId { get; set; }
    }
}
