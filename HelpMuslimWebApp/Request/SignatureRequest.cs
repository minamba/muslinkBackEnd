using System.ComponentModel.DataAnnotations;

namespace HelpMuslimWebApp.Api.Request
{
    // Modèle de signature (création, modification, aperçu)
    public class SignatureTemplateRequest
    {
        [Required(ErrorMessage = "Le nom du modèle est obligatoire.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Le nom doit contenir entre 2 et 100 caractères.")]
        public string? Name { get; set; }

        [StringLength(120, ErrorMessage = "La formule ne doit pas dépasser 120 caractères.")]
        public string? Closing { get; set; }

        [StringLength(200, ErrorMessage = "La phrase ne doit pas dépasser 200 caractères.")]
        public string? Tagline { get; set; }

        public bool ShowJobTitle { get; set; } = true;
        public bool ShowEmail { get; set; } = true;
        public bool ShowWebsite { get; set; } = true;

        [StringLength(40, ErrorMessage = "Le téléphone ne doit pas dépasser 40 caractères.")]
        public string? Phone { get; set; }

        [RegularExpression("^#[0-9a-fA-F]{6}$", ErrorMessage = "Couleur invalide (format #rrggbb).")]
        public string? Color { get; set; }

        public bool IsDefault { get; set; }
    }

    // Aperçu : les réglages en cours de saisie, pour la personne choisie
    public class SignaturePreviewRequest : SignatureTemplateRequest
    {
        [StringLength(200)]
        public string? FullName { get; set; }

        [StringLength(120)]
        public string? JobTitle { get; set; }
    }

    // Signature d'un administrateur : modèle (vide = par défaut) et fonction affichée
    public class SignatureAssignmentRequest
    {
        public int? TemplateId { get; set; }

        [StringLength(120, ErrorMessage = "La fonction ne doit pas dépasser 120 caractères.")]
        public string? JobTitle { get; set; }
    }
}
