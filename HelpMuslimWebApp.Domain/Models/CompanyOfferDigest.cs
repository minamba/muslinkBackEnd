namespace HelpMuslimWebApp.Domain.Models
{
    // Offre en attente du récapitulatif « entreprises suivies » d'un candidat, avec ce qu'il faut pour décider si elle reste pertinente
    public class CompanyDigestItem
    {
        public int Id { get; set; }
        public int CandidateProfileId { get; set; }
        public string? CandidateEmail { get; set; }
        public string? CandidateFirstName { get; set; }

        public int JobOfferId { get; set; }
        public string? OfferTitle { get; set; }
        public string? ContractType { get; set; }
        public string? City { get; set; }
        public int CompanyId { get; set; }
        public string? CompanyName { get; set; }

        // Ouverte, non pourvue, visible et entreprise active
        public bool OfferStillOpen { get; set; }
        public bool StillFollowing { get; set; }
        public bool AlreadyApplied { get; set; }
    }

    public static class CompanyDigest
    {
        // Au-delà, l'email renvoie vers « Entreprises suivies »
        public const int MaxListed = 10;
    }
}
