namespace HelpMuslimWebApp.Dal.Entities
{
    public partial class CandidateEducation
    {
        public int Id { get; set; }
        public int? CandidateProfileId { get; set; }
        public string? School { get; set; }
        public string? Degree { get; set; }
        public string? FieldOfStudy { get; set; }
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public string? Description { get; set; }

        public virtual CandidateProfile? CandidateProfile { get; set; }
    }
}
