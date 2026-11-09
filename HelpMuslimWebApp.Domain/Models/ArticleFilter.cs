namespace HelpMuslimWebApp.Domain.Models
{
    public class ArticleFilter
    {
        public string? Category { get; set; }
        public string? Search { get; set; }
        public int? ExcludeId { get; set; }
        // Date de création (publication) : du plus récent au plus ancien par défaut
        public bool OldestFirst { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 9;
    }
}
