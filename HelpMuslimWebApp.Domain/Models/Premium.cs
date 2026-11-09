namespace HelpMuslimWebApp.Domain.Models
{
    // Règles du premium (candidats) et de l'offre Recruteur Pro
    public static class Premium
    {
        // Classement chez les recruteurs : un premium gagne 10 points pour l'ordre de la liste seulement
        // (le pourcentage affiché reste le vrai). Un premium à 75 % peut passer devant un gratuit à 80 %,
        // jamais un profil à 20 % devant un profil à 95 %. Entre deux premium, la correspondance départage.
        public const int RankingBonus = 10;

        // Durées proposées dans l'administration pour offrir le premium (mois) ; « à vie » en plus
        public static readonly IReadOnlyList<int> GrantDurations = [1, 3, 6, 12];
        public static readonly DateTime Lifetime = new(9999, 12, 31, 0, 0, 0, DateTimeKind.Utc);

        // Recruteur gratuit (mode recruteurs activé) : offres actives hors bénévolat, profils correspondants visibles
        public const int FreeActiveOffers = 3;
        public const int FreeVisibleMatches = 3;
        public const int SponsorDays = 30;

        public static DateTime? Until(DateTime? grantedUntil, DateTime? paidUntil) =>
            grantedUntil == null ? paidUntil : paidUntil == null ? grantedUntil : (grantedUntil > paidUntil ? grantedUntil : paidUntil);

        public static bool IsActive(DateTime? until) => until > DateTime.UtcNow;
        public static bool IsLifetime(DateTime? until) => until?.Year >= 9999;

        public static int RankingKey(int score, bool isPremium) => isPremium ? score + RankingBonus : score;

        // Période offerte : à vie, ou {months} mois ajoutés à une période offerte encore en cours
        public static DateTime GrantUntil(DateTime? currentGrantedUntil, int? months, bool lifetime)
        {
            if (lifetime || IsLifetime(currentGrantedUntil)) return Lifetime;
            if (months == null || !GrantDurations.Contains(months.Value))
                throw new ArgumentException($"Durée invalide : {string.Join(", ", GrantDurations)} mois, ou à vie.");
            var start = IsActive(currentGrantedUntil) ? currentGrantedUntil!.Value : DateTime.UtcNow;
            return start.AddMonths(months.Value);
        }
    }

    // Abonnement Stripe en cours, renouvelé automatiquement (candidat ou recruteur)
    public class PaidSubscription
    {
        public int ProfileId { get; set; }
        public string? Email { get; set; }
        public string? FirstName { get; set; }
        public string SubscriptionId { get; set; } = string.Empty;
        public DateTime? PaidUntil { get; set; }
    }

    // Réglage du premium (administration)
    public class PremiumSettings
    {
        public bool CandidatesEnabled { get; set; }
        public int CandidateMonthlyPriceCents { get; set; }
        public bool RecruitersEnabled { get; set; }
        public int RecruiterMonthlyPriceCents { get; set; }
        public int RecruiterYearlyPriceCents { get; set; }
        public int SponsoredOfferPriceCents { get; set; }
        public bool LaunchOfferEnabled { get; set; }
        public int FounderSlots { get; set; } = Founders.DefaultSlots;
        public bool FounderAutoGrant { get; set; } = true;
        public DateTime? UpdatedAt { get; set; }
    }

    // Candidat premium (liste de l'administration)
    public class PremiumMember
    {
        public int CandidateProfileId { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public DateTime? GrantedUntil { get; set; }
        public DateTime? PaidUntil { get; set; }
        public bool CancelAtPeriodEnd { get; set; }
        public bool HasSubscription { get; set; }
    }

    // Recruteur Pro (liste de l'administration)
    public class ProMember
    {
        public int RecruiterProfileId { get; set; }
        public string? CompanyName { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public DateTime? GrantedUntil { get; set; }
        public DateTime? PaidUntil { get; set; }
        public bool IsFounder { get; set; }
        public bool CancelAtPeriodEnd { get; set; }
        public bool HasSubscription { get; set; }
    }

    // État d'un abonnement Stripe à reporter sur le candidat ou le recruteur
    public class PremiumSubscriptionState
    {
        public int ProfileId { get; set; }
        public string? CustomerId { get; set; }
        public string? SubscriptionId { get; set; }
        // null : abonnement terminé
        public DateTime? PaidUntil { get; set; }
        public bool CancelAtPeriodEnd { get; set; }
        // Périodicité : month | year
        public string? Plan { get; set; }
    }
}
