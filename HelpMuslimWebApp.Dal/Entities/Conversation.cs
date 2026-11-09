namespace HelpMuslimWebApp.Dal.Entities
{
    public partial class Conversation
    {
        public int Id { get; set; }
        public int? RecruiterProfileId { get; set; }
        public int? CandidateProfileId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? LastMessageAt { get; set; }
        // Email d'alerte « nouveau message » envoyé au candidat / au recruteur et pas encore lu :
        // un seul email par conversation tant que le destinataire ne l'a pas ouverte
        public DateTime? CandidateAlertedAt { get; set; }
        public DateTime? RecruiterAlertedAt { get; set; }

        public virtual RecruiterProfile? RecruiterProfile { get; set; }
        public virtual CandidateProfile? CandidateProfile { get; set; }
        public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
    }
}
