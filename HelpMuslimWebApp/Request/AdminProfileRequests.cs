using System.ComponentModel.DataAnnotations;

namespace HelpMuslimWebApp.Api.Request
{
    // Administration : la fiche se modifie avec l'email du compte (le compte de connexion est mis à jour en même temps)
    public class AdminRecruiterProfileRequest : RecruiterProfileRequest
    {
        [Required(ErrorMessage = "L'email est obligatoire.")]
        [EmailAddress(ErrorMessage = "Adresse email invalide.")]
        [StringLength(255)]
        public string? Email { get; set; }
    }

    public class AdminCandidateProfileRequest : CandidateProfileRequest
    {
        [Required(ErrorMessage = "L'email est obligatoire.")]
        [EmailAddress(ErrorMessage = "Adresse email invalide.")]
        [StringLength(255)]
        public string? Email { get; set; }
    }

    // Administration › Utilisateurs : identité du compte modifiée dans l'IdentityServer, recopiée dans ses fiches
    public class AccountIdentityRequest
    {
        [Required(ErrorMessage = "Le prénom est obligatoire.")]
        [StringLength(100)]
        public string? FirstName { get; set; }

        [Required(ErrorMessage = "Le nom est obligatoire.")]
        [StringLength(100)]
        public string? LastName { get; set; }

        [Required(ErrorMessage = "L'email est obligatoire.")]
        [EmailAddress(ErrorMessage = "Adresse email invalide.")]
        [StringLength(255)]
        public string? Email { get; set; }
    }

    // Administration › Utilisateurs : liste complète des comptes de test de l'IdentityServer, recopiée sur les fiches
    public class TestAccountsRequest
    {
        [Required]
        public List<string>? UserIds { get; set; }
    }
}
