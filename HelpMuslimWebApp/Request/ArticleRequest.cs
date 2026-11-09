using System.ComponentModel.DataAnnotations;

namespace HelpMuslimWebApp.Api.Request
{
    public class ArticleRequest
    {
        public int Id { get; set; }   // inclure Id pour les PUT

        [Required(ErrorMessage = "Le titre est obligatoire.")]
        [StringLength(200, MinimumLength = 5, ErrorMessage = "Le titre doit contenir entre 5 et 200 caractères.")]
        public string? Title { get; set; }

        [StringLength(400, ErrorMessage = "Le chapeau ne doit pas dépasser 400 caractères.")]
        public string? Summary { get; set; }

        [Required(ErrorMessage = "Le contenu est obligatoire.")]
        public string? Content { get; set; }

        [StringLength(60)]
        public string? Category { get; set; }

        // Chemin renvoyé par l'envoi de média (/uploads/articles/images/…)
        public string? CoverImageUrl { get; set; }
    }

    public class ArticleQueryRequest
    {
        public string? Category { get; set; }
        public string? Search { get; set; }
        public int? ExcludeId { get; set; }

        // Tri par date de création : « recent » (par défaut) ou « ancien »
        [RegularExpression("^(recent|ancien)$", ErrorMessage = "Tri invalide.")]
        public string? Sort { get; set; }

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 9;
    }
}
