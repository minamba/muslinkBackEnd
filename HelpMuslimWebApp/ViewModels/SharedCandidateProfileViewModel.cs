namespace HelpMuslimWebApp.Api.ViewModels
{
    // Fiche publique d'un candidat, ouverte par le QR code (sans date de naissance ni CV ;
    // email et téléphone uniquement si le candidat a choisi de les partager)
    public class SharedCandidateProfileViewModel
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Headline { get; set; }
        public string? City { get; set; }
        public string? Situation { get; set; }
        public string? DesiredJob { get; set; }
        public string? DesiredDomainName { get; set; }
        public string? DesiredProfessionName { get; set; }
        public string? Summary { get; set; }
        public string? PhotoUrl { get; set; }
        public string? LinkedinUrl { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<ExperienceViewModel> Experiences { get; set; } = [];
        public List<EducationViewModel> Educations { get; set; } = [];
        public List<string> Skills { get; set; } = [];
        public List<LanguageViewModel> Languages { get; set; } = [];
    }

    // État du partage de la fiche du candidat connecté
    public class CandidateShareViewModel
    {
        public bool IsEnabled { get; set; }
        public string? Token { get; set; }
        public bool ShareContact { get; set; }
    }
}
