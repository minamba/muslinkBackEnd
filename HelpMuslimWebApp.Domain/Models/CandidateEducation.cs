namespace HelpMuslimWebApp.Domain.Models
{
    public class CandidateEducation
    {
        public int Id { get; set; }
        public string? School { get; set; }
        public string? Degree { get; set; }
        public string? FieldOfStudy { get; set; }
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public string? Description { get; set; }
    }
}
