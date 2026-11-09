using System.ComponentModel.DataAnnotations;

namespace HelpMuslimWebApp.Api.Request
{
    // Candidat : rendre son profil invisible des recruteurs
    public class ProfileVisibilityRequest
    {
        [Required]
        public bool? IsHidden { get; set; }
    }
}
