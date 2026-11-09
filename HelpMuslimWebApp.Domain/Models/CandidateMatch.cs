namespace HelpMuslimWebApp.Domain.Models
{
    // Candidat et sa correspondance avec l'offre du recruteur qui lui convient le mieux
    public class CandidateMatch
    {
        public CandidateSummary Candidate { get; set; } = new();

        // 0 à 100
        public int Score { get; set; }
        public int? OfferId { get; set; }
        public string? OfferTitle { get; set; }
        public List<string> Reasons { get; set; } = [];
    }
}
