namespace HelpMuslimWebApp.Dal.Entities
{
    public partial class CandidateLanguage
    {
        public int Id { get; set; }
        public int? CandidateProfileId { get; set; }
        public string? Name { get; set; }
        public string? Level { get; set; }

        public virtual CandidateProfile? CandidateProfile { get; set; }
    }
}
