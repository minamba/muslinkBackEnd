namespace HelpMuslimWebApp.Dal.Entities
{
    // Modèle de signature des messages écrits depuis la boîte support
    public partial class SignatureTemplate
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Closing { get; set; }
        public string? Tagline { get; set; }
        public bool ShowJobTitle { get; set; }
        public bool ShowEmail { get; set; }
        public bool ShowWebsite { get; set; }
        public string? Phone { get; set; }
        public string? Color { get; set; }
        public bool IsDefault { get; set; }
        public DateTime CreatedAt { get; set; }

        public virtual ICollection<SignatureAssignment> Assignments { get; set; } = new List<SignatureAssignment>();
    }

    // Signature d'un administrateur : son modèle (vide = modèle par défaut) et la fonction affichée
    public partial class SignatureAssignment
    {
        public string UserId { get; set; } = string.Empty;
        public int? TemplateId { get; set; }
        public string? JobTitle { get; set; }
        public DateTime UpdatedAt { get; set; }

        public virtual SignatureTemplate? Template { get; set; }
    }
}
