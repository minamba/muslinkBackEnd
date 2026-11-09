namespace HelpMuslimWebApp.Dal.Entities
{
    // Couple offre / candidat déjà signalé au recruteur par l'alerte quotidienne (correspondance à 70 % au moins)
    public partial class CandidateMatchAlert
    {
        public int Id { get; set; }
        public int? JobOfferId { get; set; }
        public int? CandidateProfileId { get; set; }
        public int Score { get; set; }
        public DateTime? CreatedAt { get; set; }

        public virtual JobOffer? JobOffer { get; set; }
        public virtual CandidateProfile? CandidateProfile { get; set; }
    }
}
