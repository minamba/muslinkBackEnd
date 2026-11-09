namespace HelpMuslimWebApp.Domain.Models
{
    // Article d'actualité (contenu HTML assaini côté Api avant enregistrement)
    public class Article
    {
        public int Id { get; set; }
        public string? Title { get; set; }

        // Chapeau écrit par l'auteur (facultatif), affiché sous le titre
        public string? Summary { get; set; }

        // Extrait automatique du début du texte, pour les cartes
        public string? Excerpt { get; set; }
        public string? Content { get; set; }
        public string? Category { get; set; }
        public string? CoverImagePath { get; set; }

        // Vignette des listes : image de couverture, sinon première image du contenu
        public string? ThumbnailPath { get; set; }
        public int ReadingMinutes { get; set; }

        public string? AuthorUserId { get; set; }
        public string? AuthorName { get; set; }
        public string? UpdatedByName { get; set; }
        public DateTime? PublishedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
