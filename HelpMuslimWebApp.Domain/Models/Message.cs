namespace HelpMuslimWebApp.Domain.Models
{
    // Types de partage dans la messagerie (Message.Kind)
    public static class MessageKinds
    {
        public const string Phone = "phone";
        public const string Offer = "offer";
        public const string Cv = "cv";
    }

    public class Message
    {
        // Seuls les messages écrits se modifient : ni les partages, ni la lettre d'une candidature, ni un message supprimé
        public bool IsEditable => Kind == null && JobApplicationId == null && DeletedAt == null;

        // Son message se supprime (écrit ou partage), sauf la lettre d'une candidature (elle fait partie de la candidature)
        public bool IsDeletable => JobApplicationId == null && DeletedAt == null;

        public int Id { get; set; }
        public int? ConversationId { get; set; }
        public string? SenderUserId { get; set; }
        public string? Content { get; set; }
        public int? JobApplicationId { get; set; }
        public DateTime? SentAt { get; set; }
        public DateTime? ReadAt { get; set; }
        // Partage dans la messagerie (null : message écrit) : « phone », « offer » ou « cv »
        public string? Kind { get; set; }
        // Ce qui est partagé, en JSON (numéro ; offre : id, titre, entreprise, ville, contrat ; CV : nom du fichier)
        public string? Data { get; set; }
        // CV partagé : copie du fichier au moment de l'envoi (fichier privé)
        public string? AttachmentPath { get; set; }
        // Message écrit modifié par son auteur après l'envoi (UTC)
        public DateTime? EditedAt { get; set; }
        // Supprimé par son auteur (UTC) : texte, partage et CV joint effacés, « Message supprimé » affiché à sa place
        public DateTime? DeletedAt { get; set; }
    }
}
