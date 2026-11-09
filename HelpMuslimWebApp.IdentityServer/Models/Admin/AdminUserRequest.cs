using System.ComponentModel.DataAnnotations;
using HelpMuslimWebApp.Domain.Security;

namespace HelpMuslimWebApp.IdentityServer.Models.Admin
{
    public class AdminUserRequest
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

        [Required(ErrorMessage = "Le type de compte est obligatoire.")]
        public AccountType? AccountType { get; set; }

        public bool IsActive { get; set; } = true;

        // Compte de test (null : inchangé ; à la création, coché d'office pendant le mode test)
        public bool? IsTestAccount { get; set; }

        // Création : obligatoire. Modification : facultatif (vide = mot de passe inchangé)
        public string? Password { get; set; }

        // Rôles d'administration (comptes "Administration" uniquement)
        public List<string> Roles { get; set; } = [];
    }
}
