namespace HelpMuslimWebApp.Domain.Models
{
    // Outils de l'assistant IA des recruteurs (RecruiterAiUsage.Feature)
    public static class RecruiterAiFeatures
    {
        // Offre pré-remplie à partir de notes ou d'une fiche de poste
        public const string OfferDraft = "offer_draft";
        // Relecture d'une offre avant publication
        public const string OfferReview = "offer_review";
        // Analyse d'une offre publiée (chiffres de Muslink, puis conseils)
        public const string OfferInsights = "offer_insights";
        // Questions d'entretien préparées à partir d'une offre
        public const string InterviewQuestions = "interview_questions";
        // Brouillon de message à un candidat
        public const string MessageDraft = "message_draft";

        // Quota « offres » : rédaction, relecture, analyse et questions d'entretien comptent ensemble
        public static readonly IReadOnlyList<string> Offer = [OfferDraft, OfferReview, OfferInsights, InterviewQuestions];
        public static readonly IReadOnlyList<string> Message = [MessageDraft];
    }

    // Un appel à l'assistant IA d'un recruteur (Recruteur Pro) : ni le texte envoyé ni la réponse ne sont gardés,
    // seulement de quoi compter le quota du mois et les frais
    public class RecruiterAiUsage
    {
        public int Id { get; set; }
        public int RecruiterProfileId { get; set; }
        // UTC
        public DateTime CreatedAt { get; set; }
        public string Feature { get; set; } = RecruiterAiFeatures.OfferDraft;
        public string? Model { get; set; }
        public int InputTokens { get; set; }
        public int OutputTokens { get; set; }
    }

    // Un appel de l'assistant (lecture pour les frais)
    public record RecruiterAiUsageRow(int RecruiterProfileId, string? CompanyName, string? Name, string? Email, DateTime? ProUntil,
        string Feature, string? Model, int InputTokens, int OutputTokens, DateTime CreatedAt);

    // Tableau de bord › Frais › Recruteurs : ce que l'assistant IA a coûté sur la période
    public class RecruiterAiCostStats
    {
        public StatsScale Scale { get; set; }
        // Fenêtre en heure locale du site
        public DateTime From { get; set; }
        public DateTime To { get; set; }

        public decimal TotalUsd { get; set; }
        public int Uses { get; set; }
        public long InputTokens { get; set; }
        public long OutputTokens { get; set; }

        // Par outil : rédaction, relecture et analyse d'offres, questions d'entretien, brouillons de messages
        public int OfferDrafts { get; set; }
        public decimal OfferDraftsUsd { get; set; }
        public int OfferReviews { get; set; }
        public decimal OfferReviewsUsd { get; set; }
        public int OfferInsights { get; set; }
        public decimal OfferInsightsUsd { get; set; }
        public int InterviewQuestions { get; set; }
        public decimal InterviewQuestionsUsd { get; set; }
        public int MessageDrafts { get; set; }
        public decimal MessageDraftsUsd { get; set; }

        // Taux de conversion affiché (réglage Costs:EurPerUsd), fixé par l'Api
        public decimal EurPerUsd { get; set; }

        public List<RecruiterAiCostPoint> Points { get; set; } = [];
        // Recruteurs qui ont utilisé l'assistant sur la période, du plus coûteux au moins coûteux
        public List<RecruiterAiCostRecruiter> Recruiters { get; set; } = [];
    }

    public class RecruiterAiCostPoint
    {
        // Début du découpage, en heure locale du site
        public DateTime Start { get; set; }
        public decimal Usd { get; set; }
        public int Uses { get; set; }
    }

    public class RecruiterAiCostRecruiter
    {
        public int RecruiterProfileId { get; set; }
        public string? CompanyName { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
        public bool IsPro { get; set; }
        public int OfferDrafts { get; set; }
        public int OfferReviews { get; set; }
        public int OfferInsights { get; set; }
        public int InterviewQuestions { get; set; }
        public int MessageDrafts { get; set; }
        public long InputTokens { get; set; }
        public long OutputTokens { get; set; }
        public decimal Usd { get; set; }
        // Dernier appel sur la période (UTC)
        public DateTime? LastAt { get; set; }
    }
}
