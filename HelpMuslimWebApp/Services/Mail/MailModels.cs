namespace HelpMuslimWebApp.Api.Services.Mail
{
    // Dossiers de la boîte support affichés dans l'administration
    public enum MailFolder
    {
        Inbox,
        Sent,
        Drafts,
    }

    public record MailAddress(string? Name, string Address);

    public record MailAttachmentInfo(int Index, string FileName, string ContentType, long Size);

    public record MailSummary(
        uint Uid,
        string? FromName,
        string? FromAddress,
        IReadOnlyList<MailAddress> To,
        string? Subject,
        DateTime Date,
        bool IsRead,
        bool IsAnswered,
        bool HasAttachments,
        string? Preview);

    public record MailListResult(IReadOnlyList<MailSummary> Items, int Total, int Unread, string? Error = null);

    public record MailCounts(int Inbox, int Unread, int Sent, int Drafts, string? Error = null);

    public class MailDetail
    {
        public uint Uid { get; init; }
        public MailFolder Folder { get; init; }
        public string? FromName { get; init; }
        public string? FromAddress { get; init; }

        // « Répondre à » quand il diffère de l'expéditeur (formulaire de contact : le visiteur)
        public MailAddress? ReplyTo { get; init; }
        public IReadOnlyList<MailAddress> To { get; init; } = [];
        public IReadOnlyList<MailAddress> Cc { get; init; } = [];
        public IReadOnlyList<MailAddress> Bcc { get; init; } = [];
        public string? Subject { get; init; }
        public DateTime Date { get; init; }
        public bool IsAnswered { get; init; }

        // HTML assaini (scripts, formulaires, événements retirés ; images distantes bloquées par défaut)
        public string? Html { get; init; }
        public string? Text { get; init; }
        public bool ImagesBlocked { get; init; }
        public IReadOnlyList<MailAttachmentInfo> Attachments { get; init; } = [];

        // Brouillon : message d'origine d'une réponse, et signature ajoutée ou non
        public uint? DraftReplyToUid { get; init; }
        public bool DraftSignature { get; init; } = true;
    }

    public record MailUpload(string FileName, string ContentType, byte[] Data);

    // Message rédigé dans l'administration (nouveau, réponse, ou brouillon repris)
    public class OutgoingMail
    {
        public List<string> To { get; init; } = [];
        public List<string> Cc { get; init; } = [];
        public List<string> Bcc { get; init; } = [];
        public string Subject { get; init; } = string.Empty;

        // Texte saisi, sans la signature ni la citation (ajoutées à l'envoi)
        public string Text { get; init; } = string.Empty;

        public bool Signature { get; init; } = true;

        // Signature de l'administrateur qui écrit (son modèle, à son nom), ajoutée si Signature est vrai
        public string? SignatureHtml { get; init; }
        public string? SignatureText { get; init; }

        // Réponse : identifiant du message reçu (fil de discussion, citation, drapeau « répondu »)
        public uint? ReplyToUid { get; init; }

        // Brouillon repris : il est supprimé après l'envoi ; ses pièces jointes listées ici sont conservées
        public uint? DraftUid { get; init; }
        public List<int> KeepDraftAttachments { get; init; } = [];

        public List<MailUpload> Attachments { get; init; } = [];
    }

    public record MailSendResult(bool Sent, string? Error = null, bool CopiedToSent = false);
}
