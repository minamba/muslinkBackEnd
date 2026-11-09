namespace HelpMuslimWebApp.Api.ViewModels
{
    // Conversation vue par l'utilisateur connecté : "OtherParty" = son interlocuteur
    public class ConversationViewModel
    {
        public int Id { get; set; }
        public int? RecruiterProfileId { get; set; }
        public int? CandidateProfileId { get; set; }
        public string? OtherPartyName { get; set; }
        public string? OtherPartySubtitle { get; set; }
        public string? OtherPartyAvatarUrl { get; set; }
        public string? LastMessage { get; set; }
        public DateTime? LastMessageAt { get; set; }
        public int UnreadCount { get; set; }
        public DateTime? CreatedAt { get; set; }

        // Vue admin : les deux participants
        public string? RecruiterUserId { get; set; }
        public string? RecruiterName { get; set; }
        public string? CompanyName { get; set; }
        public string? CandidateName { get; set; }
    }
}
