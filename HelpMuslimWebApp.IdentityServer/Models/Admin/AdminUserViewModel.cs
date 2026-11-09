namespace HelpMuslimWebApp.IdentityServer.Models.Admin
{
    public class AdminUserViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? AccountType { get; set; }
        public bool IsActive { get; set; }
        public bool IsTestAccount { get; set; }
        public bool HasPassword { get; set; }
        public IEnumerable<string> Roles { get; set; } = [];
        public IEnumerable<string> ExternalLogins { get; set; } = [];
        public DateTime CreatedAt { get; set; }
    }

    public class AdminPagedResult<T>
    {
        public List<T> Items { get; set; } = [];
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }
}
