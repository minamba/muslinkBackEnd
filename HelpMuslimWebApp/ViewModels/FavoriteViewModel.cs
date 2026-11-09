namespace HelpMuslimWebApp.Api.ViewModels
{
    public class FavoriteViewModel
    {
        public int Id { get; set; }
        public int? CandidateProfileId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public CandidateSummaryViewModel? Candidate { get; set; }
    }
}
