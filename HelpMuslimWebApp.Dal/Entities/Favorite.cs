namespace HelpMuslimWebApp.Dal.Entities
{
    public partial class Favorite
    {
        public int Id { get; set; }
        public int? RecruiterProfileId { get; set; }
        public int? CandidateProfileId { get; set; }
        public DateTime? CreatedAt { get; set; }

        public virtual RecruiterProfile? RecruiterProfile { get; set; }
        public virtual CandidateProfile? CandidateProfile { get; set; }
    }
}
