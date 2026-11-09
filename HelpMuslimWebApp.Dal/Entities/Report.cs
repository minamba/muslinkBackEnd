namespace HelpMuslimWebApp.Dal.Entities
{
    // Signalement (voir Domain.Models.Report) ; supprimé avec le profil de son auteur
    public partial class Report
    {
        public int Id { get; set; }
        public string TargetType { get; set; } = string.Empty;
        public int TargetId { get; set; }
        public string TargetLabel { get; set; } = string.Empty;
        public string AuthorUserId { get; set; } = string.Empty;
        public string AuthorKind { get; set; } = string.Empty;
        public int? AuthorCandidateProfileId { get; set; }
        public int? AuthorRecruiterProfileId { get; set; }
        public string AuthorName { get; set; } = string.Empty;
        public string? AuthorEmail { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string Justification { get; set; } = string.Empty;
        public string Status { get; set; } = "new";
        public string? AdminNote { get; set; }
        public string? HandledBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? StatusChangedAt { get; set; }

        public virtual CandidateProfile? AuthorCandidateProfile { get; set; }
        public virtual RecruiterProfile? AuthorRecruiterProfile { get; set; }
    }
}
