namespace HelpMuslimWebApp.Api.ViewModels
{
    // Signalement vu par l'administration
    public class ReportViewModel
    {
        public int Id { get; set; }
        public string TargetType { get; set; } = string.Empty;
        public string TargetTypeLabel { get; set; } = string.Empty;
        public int TargetId { get; set; }
        public string TargetLabel { get; set; } = string.Empty;
        public string TargetPath { get; set; } = string.Empty;      // page de l'élément signalé sur le site
        public string AuthorKind { get; set; } = string.Empty;      // candidate | recruiter
        public string AuthorName { get; set; } = string.Empty;
        public string? AuthorEmail { get; set; }
        public string? AuthorPath { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string Justification { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string StatusLabel { get; set; } = string.Empty;
        public string? AdminNote { get; set; }
        public string? HandledBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? StatusChangedAt { get; set; }
    }
}
