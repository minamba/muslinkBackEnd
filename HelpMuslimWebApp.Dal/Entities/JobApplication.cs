namespace HelpMuslimWebApp.Dal.Entities
{
    public partial class JobApplication
    {
        public int Id { get; set; }
        public int? JobOfferId { get; set; }
        public int? CandidateProfileId { get; set; }
        public string? CoverLetter { get; set; }
        public DateTime? CreatedAt { get; set; }

        // Copie du CV jointe au moment de la candidature (le candidat peut ensuite changer celui de son profil)
        public string? CvPath { get; set; }
        public string? CvFileName { get; set; }

        // Retirée de « Mes candidatures » par le candidat (offre fermée) : le recruteur la conserve
        public DateTime? HiddenByCandidateAt { get; set; }

        public virtual JobOffer? JobOffer { get; set; }
        public virtual CandidateProfile? CandidateProfile { get; set; }
    }
}
