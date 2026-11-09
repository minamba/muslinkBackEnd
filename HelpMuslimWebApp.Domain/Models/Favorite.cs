namespace HelpMuslimWebApp.Domain.Models
{
    // Candidat mis en favori par un recruteur
    public class Favorite
    {
        public int Id { get; set; }
        public int? RecruiterProfileId { get; set; }
        public int? CandidateProfileId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public CandidateSummary? Candidate { get; set; }
    }
}
