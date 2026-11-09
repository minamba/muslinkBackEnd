namespace HelpMuslimWebApp.Domain.Models
{
    // Fil de discussion entre un recruteur et un candidat
    public class Conversation
    {
        public int Id { get; set; }
        public int? RecruiterProfileId { get; set; }
        public int? CandidateProfileId { get; set; }
        public string? RecruiterUserId { get; set; }
        public string? CandidateUserId { get; set; }
        public string? RecruiterName { get; set; }
        public string? RecruiterEmail { get; set; }
        public string? CompanyName { get; set; }
        public string? CompanyLogoPath { get; set; }
        public string? CandidateName { get; set; }
        public string? CandidateEmail { get; set; }
        public string? CandidatePhotoPath { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? LastMessageAt { get; set; }
        public string? LastMessage { get; set; }
        public int UnreadCount { get; set; }
    }
}
