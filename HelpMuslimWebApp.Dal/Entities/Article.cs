namespace HelpMuslimWebApp.Dal.Entities
{
    public partial class Article
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? Summary { get; set; }
        public string? Excerpt { get; set; }
        public string? Content { get; set; }
        public string? Category { get; set; }
        public string? CoverImagePath { get; set; }
        public string? ThumbnailPath { get; set; }
        public int? ReadingMinutes { get; set; }
        public string? AuthorUserId { get; set; }
        public string? AuthorName { get; set; }
        public string? UpdatedByName { get; set; }
        public DateTime? PublishedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
