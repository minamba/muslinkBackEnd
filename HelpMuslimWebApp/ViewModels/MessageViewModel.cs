namespace HelpMuslimWebApp.Api.ViewModels
{
    public class MessageViewModel
    {
        public int Id { get; set; }
        public int? ConversationId { get; set; }
        public string? SenderUserId { get; set; }
        public bool IsMine { get; set; }
        public string? Content { get; set; }
        public int? JobApplicationId { get; set; }
        public DateTime? SentAt { get; set; }
        public DateTime? ReadAt { get; set; }
        // Modifié après l'envoi (mention « modifié »)
        public DateTime? EditedAt { get; set; }
        // Supprimé par son auteur : « Message supprimé » affiché à sa place
        public DateTime? DeletedAt { get; set; }

        // Partage (null : message écrit) : « phone », « offer » ou « cv », et ce qui est partagé
        public string? Kind { get; set; }
        public MessageShareViewModel? Share { get; set; }

        // Renseignés dans les notifications temps réel (toast "Nouveau message de …")
        public string? SenderName { get; set; }
        public string? SenderSubtitle { get; set; }
        public string? SenderAvatarUrl { get; set; }
    }

    // Contenu d'un partage dans la messagerie, gardé en JSON dans le message (tel qu'il était à l'envoi)
    public class MessageShareViewModel
    {
        // Numéro de téléphone
        public string? Phone { get; set; }

        // Fiche de poste
        public int? OfferId { get; set; }
        public string? OfferTitle { get; set; }
        public string? CompanyName { get; set; }
        public string? City { get; set; }
        public string? ContractType { get; set; }

        // CV : nom du fichier envoyé (le fichier se lit par /conversations/{id}/messages/{messageId}/cv)
        public string? FileName { get; set; }
    }
}
