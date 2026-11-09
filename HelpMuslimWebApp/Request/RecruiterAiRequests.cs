using System.ComponentModel.DataAnnotations;

namespace HelpMuslimWebApp.Api.Request
{
    // Offre à pré-remplir : notes du recruteur et/ou fiche de poste (PDF, Word .docx ou texte)
    public class OfferDraftRequest
    {
        [StringLength(8000, ErrorMessage = "Vos notes font au plus 8 000 caractères.")]
        public string? Notes { get; set; }
        public IFormFile? File { get; set; }
    }

    // Offre à relire : le formulaire tel qu'il est
    public class OfferReviewRequest
    {
        [StringLength(150)]
        public string? Title { get; set; }
        [StringLength(8000)]
        public string? Description { get; set; }
        public string? ContractType { get; set; }
        public int? DomainId { get; set; }
        public int? ProfessionId { get; set; }
        public List<string>? RequiredSkills { get; set; }
        public List<string>? BonusSkills { get; set; }
        [StringLength(150)]
        public string? City { get; set; }
        public bool? IsRemote { get; set; }
        [StringLength(150)]
        public string? Salary { get; set; }
        [StringLength(100)]
        public string? Duration { get; set; }
        public DateOnly? StartDate { get; set; }
        public int? ExperienceYears { get; set; }
    }

    // Brouillon de message : intention, consigne du recruteur, brouillon en cours (à améliorer)
    public class MessageDraftRequest
    {
        // reply | interview | details | decline | improve | free
        [Required(ErrorMessage = "Choisissez le type de message.")]
        public string? Intent { get; set; }
        [StringLength(1000, ErrorMessage = "La consigne fait au plus 1 000 caractères.")]
        public string? Instructions { get; set; }
        [StringLength(5000)]
        public string? CurrentDraft { get; set; }
    }
}
