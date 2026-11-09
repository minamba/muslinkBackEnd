namespace HelpMuslimWebApp.Dal.Entities
{
    public partial class CandidateExperience
    {
        public int Id { get; set; }
        public int? CandidateProfileId { get; set; }
        public string? Title { get; set; }
        public string? Company { get; set; }
        public string? City { get; set; }
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public bool? IsCurrent { get; set; }
        public string? Description { get; set; }

        public virtual CandidateProfile? CandidateProfile { get; set; }
    }
}
