namespace HelpMuslimWebApp.Domain.Models
{
    // Offre et sa correspondance avec les vœux du candidat (« Ce que je cherche »)
    public class WishMatch
    {
        public JobOffer Offer { get; set; } = new();

        // 0 à 100
        public int Score { get; set; }
        public List<WishCriterion> Criteria { get; set; } = [];
    }

    // Critère évalué : Credit = 1 (rempli), 0.5 (en partie) ou 0 (non rempli)
    // Kind : contract | job | city | salary | remote (analyse d'une offre : ce qui écarte les candidats intéressés)
    public record WishCriterion(string Label, double Weight, double Credit, string Kind = "");
}
