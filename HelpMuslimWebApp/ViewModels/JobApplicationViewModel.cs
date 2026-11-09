namespace HelpMuslimWebApp.Api.ViewModels
{
    public class JobApplicationViewModel
    {
        public int Id { get; set; }
        public int? JobOfferId { get; set; }
        public string? OfferTitle { get; set; }
        public string? OfferStatus { get; set; }
        public string? OfferContractType { get; set; }
        public int? RecruiterProfileId { get; set; }
        public string? CompanyName { get; set; }
        public string? CompanyLogoUrl { get; set; }
        public int? CandidateProfileId { get; set; }
        public CandidateSummaryViewModel? Candidate { get; set; }

        // Candidatures reçues (recruteur) : correspondance du candidat avec l'offre (0 à 100)
        public int? MatchScore { get; set; }
        // Listes du recruteur : compétences de l'offre que le candidat a (CandidateSkill) ou qui lui manquent (null)
        public List<SkillCheckViewModel>? Skills { get; set; }
        // Vue recruteur : il a déjà écrit à ce candidat dans la messagerie
        public bool InDiscussion { get; set; }
        public string? CoverLetter { get; set; }

        // CV joint à la candidature (téléchargeable par le recruteur de l'offre)
        public bool HasCv { get; set; }
        public string? CvFileName { get; set; }
        public DateTime? OfferClosedAt { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
