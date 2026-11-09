namespace HelpMuslimWebApp.IdentityServer.Models.Admin
{
    public class AdminRoleViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string? Name { get; set; }
        public string? Description { get; set; }
        public bool IsSystem { get; set; }

        // SuperAdmin : toutes les permissions, non modifiable
        public bool HasAllPermissions { get; set; }
        public IEnumerable<string> Permissions { get; set; } = [];
        public int UsersCount { get; set; }
    }
}
