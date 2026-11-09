using System.ComponentModel.DataAnnotations;

namespace HelpMuslimWebApp.Api.Request
{
    public class JobApplicationRequest
    {
        [Required(ErrorMessage = "L'offre est obligatoire.")]
        public int? JobOfferId { get; set; }

        [Required(ErrorMessage = "La lettre de motivation est obligatoire.")]
        [StringLength(5000, MinimumLength = 50, ErrorMessage = "La lettre de motivation doit contenir entre 50 et 5000 caractères.")]
        public string? CoverLetter { get; set; }

        // Joindre une copie du CV du profil à cette candidature (coché par défaut)
        public bool AttachCv { get; set; } = true;
    }
}
