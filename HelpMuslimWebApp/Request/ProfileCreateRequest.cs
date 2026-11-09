using System.ComponentModel.DataAnnotations;

namespace HelpMuslimWebApp.Api.Request
{
    // Admin : création du profil métier d'un compte créé dans l'IdentityServer
    public class ProfileCreateRequest
    {
        [Required]
        [StringLength(64)]
        public string? UserId { get; set; }

        [Required(ErrorMessage = "Le prénom est obligatoire.")]
        public string? FirstName { get; set; }

        public string? LastName { get; set; }

        [Required(ErrorMessage = "L'email est obligatoire.")]
        [EmailAddress(ErrorMessage = "Adresse email invalide.")]
        public string? Email { get; set; }

        // Recruteur uniquement
        public string? CompanyName { get; set; }
    }
}
