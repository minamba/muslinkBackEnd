namespace HelpMuslimWebApp.Domain.Models
{
    // Modèle de signature (Administration › Boîte mail › Signatures)
    public class SignatureTemplate
    {
        public int Id { get; set; }
        public string? Name { get; set; }

        // Formule avant la signature (« Cordialement, »)
        public string? Closing { get; set; }

        // Phrase en italique sous les coordonnées
        public string? Tagline { get; set; }
        public bool ShowJobTitle { get; set; } = true;
        public bool ShowEmail { get; set; } = true;
        public bool ShowWebsite { get; set; } = true;
        public string? Phone { get; set; }

        // Couleur du nom (#rrggbb)
        public string? Color { get; set; }
        public bool IsDefault { get; set; }

        // Administrateurs qui l'utilisent explicitement (le modèle par défaut sert aussi à tous les autres)
        public int AssignedCount { get; set; }
    }

    // Signature d'un administrateur (identifiant IdentityServer) : modèle choisi (null = par défaut) et fonction affichée
    public class SignatureAssignment
    {
        public string UserId { get; set; } = string.Empty;
        public int? TemplateId { get; set; }
        public string? JobTitle { get; set; }
    }
}
