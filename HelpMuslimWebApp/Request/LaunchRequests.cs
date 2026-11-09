using System.ComponentModel.DataAnnotations;

namespace HelpMuslimWebApp.Api.Request
{
    // Administration : offre de lancement
    public class LaunchSettingsRequest
    {
        [Required]
        public bool? Enabled { get; set; }

        [Required, Range(0, 1000, ErrorMessage = "Le nombre de places doit être compris entre 0 et 1000.")]
        public int? Slots { get; set; }

        [Required]
        public bool? AutoGrant { get; set; }
    }

    // Administration : identité (RNA d'une association, SIRET non vérifiable automatiquement) validée ou non
    public class IdentityValidationRequest
    {
        [Required]
        public bool? Verified { get; set; }
    }
}
