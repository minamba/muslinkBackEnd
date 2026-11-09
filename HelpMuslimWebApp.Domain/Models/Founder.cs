namespace HelpMuslimWebApp.Domain.Models
{
    // Offre de lancement : les premières entreprises (SIRET vérifié ou RNA validé, email confirmé, une offre publiée)
    // deviennent entreprises fondatrices, Recruteur Pro offert à vie
    public static class Founders
    {
        public const int DefaultSlots = 50;
        public const int MaxSlots = 1000;
    }

    // Aliases : noms officiels normalisés séparés par « | » (null : RNA à valider par l'équipe)
    public record CompanyIdentity(string? Siret, string? Siren, string? Rna, string? LegalName, string? Aliases, DateTime? VerifiedAt);

    // Ligne de la revue des entreprises fondatrices (administration)
    public class FounderReview
    {
        public int RecruiterProfileId { get; set; }
        public string? CompanyName { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public bool IsFounder { get; set; }
        public DateTime? FounderSince { get; set; }
        public DateTime? FounderRevokedAt { get; set; }
        public string? Siret { get; set; }
        public string? Rna { get; set; }
        public string? LegalName { get; set; }
        public DateTime? IdentityVerifiedAt { get; set; }
        public bool IsEmailConfirmed { get; set; }
        public string? SignupIpHash { get; set; }
        public DateTime? FirstOfferAt { get; set; }
        public int OffersCount { get; set; }
        public bool IsDisabled { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
