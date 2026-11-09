using Microsoft.AspNetCore.Identity;

namespace HelpMuslimWebApp.IdentityServer.Models
{
    public class ApplicationRole : IdentityRole
    {
        public ApplicationRole() { }

        public ApplicationRole(string roleName) : base(roleName) { }

        public string? Description { get; set; }

        // Rôle système (SuperAdmin, Admin) : non supprimable
        public bool IsSystem { get; set; }
    }
}
