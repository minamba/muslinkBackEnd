namespace HelpMuslimWebApp.IdentityServer.Models
{
    // Lien de démonstration (muslink.fr/demo?cle=…) : donne accès aux données de démo tant qu'il n'a pas expiré
    // et que la démo est activée. Supprimé = révoqué.
    public class DemoLink
    {
        public int Id { get; set; }
        public string Key { get; set; } = string.Empty;

        // À qui le lien a été donné (ex. « Partenaire X »)
        public string Label { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // null : sans limite
        public DateTime? ExpiresAt { get; set; }
        public DateTime? LastUsedAt { get; set; }
        public string? CreatedBy { get; set; }
    }

    // Comptes de démonstration : tous le même mot de passe (connexion possible seulement depuis un lien de démo)
    public static class DemoAccounts
    {
        public const string Password = "Demo1234!";
        public const string Claim = "demo_account";

        // Durées proposées à la création d'un lien
        public static readonly IReadOnlyDictionary<string, TimeSpan?> Durations = new Dictionary<string, TimeSpan?>
        {
            ["24h"] = TimeSpan.FromHours(24),
            ["48h"] = TimeSpan.FromHours(48),
            ["7d"] = TimeSpan.FromDays(7),
            ["30d"] = TimeSpan.FromDays(30),
            ["unlimited"] = null,
        };
    }
}
