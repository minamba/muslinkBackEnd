using System.ComponentModel.DataAnnotations;

namespace HelpMuslimWebApp.Api.Request
{
    // Formulaire de contact (public)
    public class ContactRequest
    {
        // Motifs proposés par le formulaire (même liste côté front : CONTACT_REASONS)
        public static readonly string[] Reasons =
        [
            "Question générale",
            "Je suis candidat",
            "Je suis recruteur",
            "Mot de passe oublié ou accès au compte",
            "Problème technique",
            "Mon compte et mes données",
            "Partenariat ou presse",
            "Autre",
        ];

        [Required(ErrorMessage = "Votre nom est obligatoire.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Le nom doit contenir entre 2 et 100 caractères.")]
        public string? Name { get; set; }

        [Required(ErrorMessage = "Votre email est obligatoire.")]
        [EmailAddress(ErrorMessage = "Adresse email invalide.")]
        [StringLength(200)]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Choisissez un motif.")]
        public string? Reason { get; set; }

        [Required(ErrorMessage = "Le sujet est obligatoire.")]
        [StringLength(150, MinimumLength = 3, ErrorMessage = "Le sujet doit contenir entre 3 et 150 caractères.")]
        public string? Subject { get; set; }

        [Required(ErrorMessage = "Le message est obligatoire.")]
        [StringLength(4000, MinimumLength = 20, ErrorMessage = "Le message doit contenir entre 20 et 4000 caractères.")]
        public string? Message { get; set; }

        // Champ piège invisible pour les humains : rempli uniquement par les robots
        public string? Website { get; set; }
    }
}
