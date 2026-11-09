namespace HelpMuslimWebApp.Domain.Models
{
    // Public d'un abonnement : premium candidat ou Recruteur Pro
    public static class SubscriptionAudiences
    {
        public const string Candidate = "candidate";
        public const string Recruiter = "recruiter";
    }

    // Journal des abonnements : ce qui a changé, et quand
    public static class SubscriptionEventKinds
    {
        public const string Subscribed = "subscribed";
        public const string CancelRequested = "cancel-requested";
        public const string CancelReverted = "cancel-reverted";
        public const string Ended = "ended";
        public const string Granted = "granted";
        public const string GrantRemoved = "grant-removed";
    }

    // Une ligne du journal (OccurredAt en UTC)
    public record SubscriptionEventRow(int ProfileId, string Kind, string? Plan, DateTime? GrantedUntil, DateTime OccurredAt, bool Backfilled);

    // Profil existant : nom affiché (personne ou entreprise) et date d'inscription (UTC)
    public record SubscriptionProfileRow(int Id, string? Name, DateTime? CreatedAt);

    public class SubscriptionPoint
    {
        // Début du découpage, en heure locale du site (sans fuseau)
        public DateTime Start { get; set; }
        public int NewSubscribers { get; set; }
        public int Ended { get; set; }
        public int CancelRequests { get; set; }

        // À la fin du découpage ; null s'il n'a pas encore commencé (période en cours)
        public int? Active { get; set; }
        public int? Free { get; set; }
    }

    public record PlanCount(string? Plan, int Count);

    // Dernier mouvement de la période (OccurredAt en heure locale)
    public record SubscriptionMovement(int ProfileId, string? Name, string Kind, string? Plan, DateTime OccurredAt);

    public class AudienceSubscriptionStats
    {
        // Mouvements de la période
        public int NewSubscribers { get; set; }
        public int Ended { get; set; }
        public int CancelRequests { get; set; }
        // Demandes de résiliation annulées par l'abonné (il garde son abonnement)
        public int CancelReverted { get; set; }
        public List<PlanCount> NewByPlan { get; set; } = [];

        // État à la fin de la période (maintenant pour la période en cours)
        public int Active { get; set; }
        // Dont résiliation programmée : actifs jusqu'à la fin de la période payée
        public int ActiveCancelling { get; set; }
        // Premium offert par l'administration, sans abonnement payant
        public int Offered { get; set; }
        public int Free { get; set; }
        public int Accounts { get; set; }

        public List<SubscriptionPoint> Points { get; set; } = [];
        public List<SubscriptionMovement> Latest { get; set; } = [];
    }

    public class SubscriptionStats
    {
        public StatsScale Scale { get; set; }
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        // Début du journal (heure locale) : avant, les mouvements ne sont pas connus
        public DateTime? TrackedSince { get; set; }
        public AudienceSubscriptionStats Candidates { get; set; } = new();
        public AudienceSubscriptionStats Recruiters { get; set; } = new();
    }
}
