namespace HelpMuslimWebApp.Dal.Entities
{
    // Offre pourvue grâce à la plateforme : compteur cumulatif du bandeau "en direct" de l'accueil.
    // Les informations sont recopiées pour survivre à la suppression de l'offre (le compteur ne diminue jamais).
    public partial class OfferFill
    {
        public int Id { get; set; }
        public int? JobOfferId { get; set; }
        public int? RecruiterProfileId { get; set; }
        public string? Title { get; set; }
        public string? CompanyName { get; set; }
        public string? City { get; set; }
        public string? ContractType { get; set; }
        public DateTime? FilledAt { get; set; }

        public virtual JobOffer? JobOffer { get; set; }
        public virtual RecruiterProfile? RecruiterProfile { get; set; }
    }
}
