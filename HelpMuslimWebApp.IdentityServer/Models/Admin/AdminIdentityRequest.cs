using System.ComponentModel.DataAnnotations;

namespace HelpMuslimWebApp.IdentityServer.Models.Admin
{
    // Identité d'un compte candidat / recruteur modifiée depuis sa fiche de profil (administration)
    public class AdminIdentityRequest
    {
        [Required(ErrorMessage = "L'email est obligatoire.")]
        [EmailAddress(ErrorMessage = "Adresse email invalide.")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Le prénom est obligatoire.")]
        [StringLength(100)]
        public string? FirstName { get; set; }

        [Required(ErrorMessage = "Le nom est obligatoire.")]
        [StringLength(100)]
        public string? LastName { get; set; }

        [StringLength(30)]
        public string? PhoneNumber { get; set; }
    }
}
