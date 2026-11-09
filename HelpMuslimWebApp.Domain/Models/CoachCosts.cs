namespace HelpMuslimWebApp.Domain.Models
{
    // Tarifs Anthropic en dollars par million de tokens (entrée, sortie). Les tokens de réflexion du modèle sont comptés en sortie.
    // Modèle inconnu : tarif de Claude Sonnet 5.5, le modèle du coach. À tenir à jour si Anthropic change ses prix.
    public static class CoachPricing
    {
        public const string DefaultModel = "claude-sonnet-5-5";

        private static readonly Dictionary<string, (decimal Input, decimal Output)> Prices = new(StringComparer.OrdinalIgnoreCase)
        {
            ["claude-sonnet-5-5"] = (2m, 10m),
            ["claude-sonnet-5"] = (2m, 10m),
            ["claude-opus-5-5"] = (4m, 20m),
            ["claude-opus-5"] = (5m, 25m),
            ["claude-opus-4-8"] = (5m, 25m),
            ["claude-haiku-5-5"] = (0.10m, 0.50m),
        };

        public static (decimal Input, decimal Output) PriceOf(string? model) =>
            model != null && Prices.TryGetValue(model, out var price) ? price : Prices[DefaultModel];

        public static decimal CostUsd(string? model, long inputTokens, long outputTokens)
        {
            var (input, output) = PriceOf(model);
            return (inputTokens * input + outputTokens * output) / 1_000_000m;
        }
    }

    // Une analyse du coach (lecture pour les frais)
    public record CoachUsageRow(int CandidateProfileId, string? Name, string? Email, DateTime? PremiumUntil, bool ByAdmin,
        string? Model, int InputTokens, int OutputTokens, DateTime CreatedAt);

    // Tableau de bord › Frais : ce que le coach IA a coûté sur la période
    public class CoachCostStats
    {
        public StatsScale Scale { get; set; }
        // Fenêtre en heure locale du site
        public DateTime From { get; set; }
        public DateTime To { get; set; }

        public decimal TotalUsd { get; set; }
        public int Analyses { get; set; }
        public long InputTokens { get; set; }
        public long OutputTokens { get; set; }

        // Lancées par les candidats eux-mêmes / par le super admin depuis l'administration
        public decimal CandidatesUsd { get; set; }
        public int CandidateAnalyses { get; set; }
        public decimal AdminUsd { get; set; }
        public int AdminAnalyses { get; set; }

        // Taux de conversion affiché (réglage Costs:EurPerUsd), fixé par l'Api
        public decimal EurPerUsd { get; set; }

        public List<CoachCostPoint> Points { get; set; } = [];
        // Candidats qui ont lancé au moins une analyse sur la période, du plus coûteux au moins coûteux
        public List<CoachCostCandidate> Candidates { get; set; } = [];
    }

    public class CoachCostPoint
    {
        // Début du découpage, en heure locale du site
        public DateTime Start { get; set; }
        public decimal Usd { get; set; }
        public int Analyses { get; set; }
    }

    public class CoachCostCandidate
    {
        public int CandidateProfileId { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
        public bool IsPremium { get; set; }
        public int Analyses { get; set; }
        public long InputTokens { get; set; }
        public long OutputTokens { get; set; }
        public decimal Usd { get; set; }
        // Analyses du super admin sur ce candidat (comptées à part, pas dans Usd)
        public int AdminAnalyses { get; set; }
        public decimal AdminUsd { get; set; }
        // Dernière analyse du candidat sur la période (UTC)
        public DateTime? LastAt { get; set; }
    }
}
