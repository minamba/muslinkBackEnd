namespace HelpMuslimWebApp.Dal.Entities
{
    // Entreprise (profil recruteur) suivie par un candidat : alerte à chaque nouvelle offre
    public partial class CompanyFollow
    {
        public int Id { get; set; }
        public int? CandidateProfileId { get; set; }
        public int? RecruiterProfileId { get; set; }
        public DateTime? CreatedAt { get; set; }

        public virtual CandidateProfile? CandidateProfile { get; set; }
        public virtual RecruiterProfile? RecruiterProfile { get; set; }
    }
}
