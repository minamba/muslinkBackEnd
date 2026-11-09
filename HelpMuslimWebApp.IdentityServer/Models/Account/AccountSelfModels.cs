using System.ComponentModel.DataAnnotations;

namespace HelpMuslimWebApp.IdentityServer.Models.Account
{
    // Compte de l'utilisateur connecté (page « Mon compte »)
    public class AccountSelfViewModel
    {
        public string? Email { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? AccountType { get; set; }

        // Compte créé via Google : pas de mot de passe tant qu'il n'en a pas défini un
        public bool HasPassword { get; set; }
        // Adresse confirmée (lien de l'email, ou compte Google)
        public bool EmailConfirmed { get; set; }
        public List<string> ExternalLogins { get; set; } = [];
    }

    public class AccountSelfRequest
    {
        [Required(ErrorMessage = "Le prénom est obligatoire."), StringLength(100)]
        public string? FirstName { get; set; }

        [Required(ErrorMessage = "Le nom est obligatoire."), StringLength(100)]
        public string? LastName { get; set; }

        [Required(ErrorMessage = "L'email est obligatoire."), EmailAddress(ErrorMessage = "Adresse email invalide.")]
        public string? Email { get; set; }

        [Phone(ErrorMessage = "Numéro de téléphone invalide."), StringLength(30)]
        public string? PhoneNumber { get; set; }
    }

    public class ChangePasswordRequest
    {
        // Obligatoire si le compte a déjà un mot de passe
        public string? CurrentPassword { get; set; }

        [Required(ErrorMessage = "Le nouveau mot de passe est obligatoire.")]
        public string? NewPassword { get; set; }
    }

    public class DeleteAccountRequest
    {
        // Compte avec mot de passe : confirmation par le mot de passe
        public string? Password { get; set; }

        // Compte sans mot de passe (Google) : confirmation en recopiant son email
        public string? EmailConfirmation { get; set; }
    }
}
