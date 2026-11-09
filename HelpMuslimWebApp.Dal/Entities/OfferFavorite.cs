namespace HelpMuslimWebApp.Dal.Entities
{
    // Offre mise en favori par un candidat
    public partial class OfferFavorite
    {
        public int Id { get; set; }
        public int? CandidateProfileId { get; set; }
        public int? JobOfferId { get; set; }
        public DateTime? CreatedAt { get; set; }

        public virtual CandidateProfile? CandidateProfile { get; set; }
        public virtual JobOffer? JobOffer { get; set; }
    }
}
