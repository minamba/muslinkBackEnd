namespace HelpMuslimWebApp.Dal.Entities
{
    // Page entreprise : photo déposée sur Muslink, ou vidéo YouTube / Vimeo (seul l'identifiant est gardé, la vidéo reste chez eux)
    public partial class CompanyMedia
    {
        public int Id { get; set; }
        public int? RecruiterProfileId { get; set; }
        // « photo » ou « video »
        public string Kind { get; set; } = "photo";
        // Photo : chemin du fichier public (/uploads/companies/photos/…)
        public string? Path { get; set; }
        // Vidéo : « youtube » ou « vimeo », et son identifiant (Vimeo non répertoriée : « numéro/clé »)
        public string? VideoProvider { get; set; }
        public string? VideoId { get; set; }
        // Vidéo : miniature copiée chez nous à l'ajout (/uploads/companies/thumbnails/…), pour ne contacter la plateforme qu'à la lecture
        public string? ThumbnailUrl { get; set; }
        // Légende de la photo, ou titre de la vidéo (« Rencontrez Patrick, technicien SAV »)
        public string? Title { get; set; }
        // Vidéo : citation de la personne filmée
        public string? Quote { get; set; }
        public int Position { get; set; }
        public DateTime? CreatedAt { get; set; }

        public virtual RecruiterProfile? RecruiterProfile { get; set; }
    }
}
