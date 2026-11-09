namespace HelpMuslimWebApp.Dal.Entities
{
    // Journal des abonnements (Tableau de bord › Abonnements) : une ligne par changement, jamais modifiée.
    // Sans clé étrangère : l'historique survit à la suppression du profil.
    public partial class SubscriptionEvent
    {
        public long Id { get; set; }

        // candidate | recruiter
        public string Audience { get; set; } = string.Empty;
        public int ProfileId { get; set; }

        // subscribed | cancel-requested | cancel-reverted | ended | granted | grant-removed
        public string Kind { get; set; } = string.Empty;

        public string? SubscriptionId { get; set; }

        // Abonnement : month | year
        public string? Plan { get; set; }

        // Premium offert (granted) : jusqu'à cette date
        public DateTime? GrantedUntil { get; set; }

        // UTC
        public DateTime OccurredAt { get; set; }

        // État déjà en place au début du suivi : compte dans les abonnements actifs, pas dans les nouveaux abonnés
        public bool Backfilled { get; set; }
    }
}
