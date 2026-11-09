namespace HelpMuslimWebApp.Api.ViewModels
{
    // Fiche candidature : lettre de motivation + offre + profil complet du candidat
    public class JobApplicationDetailViewModel
    {
        public JobApplicationViewModel? Application { get; set; }
        public JobOfferViewModel? Offer { get; set; }
        public CandidateProfileViewModel? Candidate { get; set; }
        // Recruteur de l'offre et administration : correspondance du candidat avec l'offre, compétence par compétence
        public ApplicationMatchViewModel? Match { get; set; }
    }

    public class ApplicationMatchViewModel
    {
        public int Score { get; set; }
        public List<SkillCheckViewModel> Skills { get; set; } = [];
    }

    // Compétence de l'offre : possédée par le candidat (CandidateSkill = son intitulé dans le profil) ou manquante (null)
    public class SkillCheckViewModel
    {
        public string Skill { get; set; } = string.Empty;
        public bool Required { get; set; }
        public string? CandidateSkill { get; set; }
    }
}
