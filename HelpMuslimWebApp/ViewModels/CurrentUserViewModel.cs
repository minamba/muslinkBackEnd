namespace HelpMuslimWebApp.Api.ViewModels
{
    public class CurrentUserViewModel
    {
        public string? Id { get; set; }
        public string? Email { get; set; }
        public string? Name { get; set; }
        public string? AccountType { get; set; }
        public IEnumerable<string> Roles { get; set; } = [];
        public IEnumerable<string> Permissions { get; set; } = [];

        // Profil métier créé automatiquement à la première connexion
        public int? CandidateProfileId { get; set; }
        public int? RecruiterProfileId { get; set; }
    }
}
