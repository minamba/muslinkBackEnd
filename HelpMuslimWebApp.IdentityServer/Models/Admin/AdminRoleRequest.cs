using System.ComponentModel.DataAnnotations;

namespace HelpMuslimWebApp.IdentityServer.Models.Admin
{
    public class AdminRoleRequest
    {
        [Required(ErrorMessage = "Le nom du rôle est obligatoire.")]
        [StringLength(64, MinimumLength = 2)]
        [RegularExpression(@"^[\p{L}0-9 _\-]+$", ErrorMessage = "Le nom ne peut contenir que des lettres, chiffres, espaces, - et _.")]
        public string? Name { get; set; }

        [StringLength(255)]
        public string? Description { get; set; }

        public List<string> Permissions { get; set; } = [];
    }
}
