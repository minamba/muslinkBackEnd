using System.ComponentModel.DataAnnotations;

namespace HelpMuslimWebApp.Api.Request
{
    // Message écrit depuis Administration › Boîte mail (multipart : champs + pièces jointes)
    public class AdminMailRequest
    {
        public List<string> To { get; set; } = [];
        public List<string> Cc { get; set; } = [];
        public List<string> Bcc { get; set; } = [];

        [StringLength(300, ErrorMessage = "L'objet ne doit pas dépasser 300 caractères.")]
        public string? Subject { get; set; }

        [StringLength(50000, ErrorMessage = "Le message est trop long.")]
        public string? Text { get; set; }

        public bool Signature { get; set; } = true;

        // Réponse à ce message de la réception
        public uint? ReplyToUid { get; set; }

        // Brouillon repris (supprimé après l'envoi), et ses pièces jointes à conserver
        public uint? DraftUid { get; set; }
        public List<int> KeepDraftAttachments { get; set; } = [];

        public List<IFormFile> Files { get; set; } = [];
    }

    public class AdminMailDeleteRequest
    {
        [Required]
        public string Folder { get; set; } = "inbox";

        public List<uint> Uids { get; set; } = [];
    }

    public class AdminMailReadRequest
    {
        public bool Read { get; set; }
    }
}
