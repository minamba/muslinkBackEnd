namespace HelpMuslimWebApp.Dal.Entities
{
    public partial class CandidateSkill
    {
        public int Id { get; set; }
        public int? CandidateProfileId { get; set; }
        public string? Name { get; set; }

        public virtual CandidateProfile? CandidateProfile { get; set; }
    }
}
