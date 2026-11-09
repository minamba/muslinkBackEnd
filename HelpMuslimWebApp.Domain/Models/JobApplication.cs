namespace HelpMuslimWebApp.Domain.Models
{
    // Candidature d'un candidat à une offre (lettre de motivation obligatoire)
    public class JobApplication
    {
        public int Id { get; set; }
        public int? JobOfferId { get; set; }
        public int? CandidateProfileId { get; set; }
        public string? CoverLetter { get; set; }
        public DateTime? CreatedAt { get; set; }

        // CV joint à la candidature (copie figée au moment de postuler)
        public string? CvPath { get; set; }
        public string? CvFileName { get; set; }

        public string? OfferTitle { get; set; }
        public string? OfferContractType { get; set; }
        public bool? OfferIsOpen { get; set; }
        public DateTime? OfferCreatedAt { get; set; }
        public DateTime? OfferClosedAt { get; set; }
        public DateTime? OfferInDiscussionAt { get; set; }
        public bool? OfferIsFilled { get; set; }
        public int? RecruiterProfileId { get; set; }
        public string? CompanyName { get; set; }
        public string? CompanyLogoPath { get; set; }
        public CandidateSummary? Candidate { get; set; }

        public string OfferStatus => OfferStatuses.Compute(OfferIsOpen, OfferCreatedAt, OfferInDiscussionAt, OfferIsFilled);
    }
}
