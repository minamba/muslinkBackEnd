namespace HelpMuslimWebApp.Dal.Entities
{
    // Nouvelle offre d'une entreprise suivie, en attente du récapitulatif quotidien d'un candidat gratuit (mode premium actif).
    // SentAt : traitée par le récapitulatif (envoyée, ou écartée parce qu'elle n'était plus pertinente)
    public partial class CompanyOfferDigest
    {
        public int Id { get; set; }
        public int? CandidateProfileId { get; set; }
        public int? JobOfferId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? SentAt { get; set; }

        public virtual CandidateProfile? CandidateProfile { get; set; }
        public virtual JobOffer? JobOffer { get; set; }
    }
}
