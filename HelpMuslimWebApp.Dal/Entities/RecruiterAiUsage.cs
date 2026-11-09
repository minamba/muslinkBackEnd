namespace HelpMuslimWebApp.Dal.Entities
{
    // Appel à l'assistant IA d'un recruteur (supprimé avec son profil) : seulement l'outil, le modèle et les tokens
    public partial class RecruiterAiUsage
    {
        public int Id { get; set; }
        public int RecruiterProfileId { get; set; }
        // UTC
        public DateTime CreatedAt { get; set; }
        // offer_draft | offer_review | message_draft
        public string Feature { get; set; } = string.Empty;
        public string? Model { get; set; }
        public int InputTokens { get; set; }
        public int OutputTokens { get; set; }

        public virtual RecruiterProfile? RecruiterProfile { get; set; }
    }
}
