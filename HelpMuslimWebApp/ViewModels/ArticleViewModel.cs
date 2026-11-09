namespace HelpMuslimWebApp.Api.ViewModels
{
    public class ArticleViewModel
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? Summary { get; set; }
        public string? Excerpt { get; set; }

        // HTML assaini (absent dans les listes)
        public string? Content { get; set; }
        public string? Category { get; set; }
        public string? CoverImageUrl { get; set; }
        public string? ThumbnailUrl { get; set; }
        public int ReadingMinutes { get; set; }
        public string? AuthorName { get; set; }
        public string? UpdatedByName { get; set; }
        public DateTime? PublishedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    // Média envoyé depuis l'éditeur
    public class ArticleMediaViewModel
    {
        public string? Url { get; set; }
        public string? FileName { get; set; }
        public string? Kind { get; set; }   // image | video | document
    }
}
