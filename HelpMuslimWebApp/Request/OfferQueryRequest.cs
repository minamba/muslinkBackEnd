namespace HelpMuslimWebApp.Api.Request
{
    // Filtres du fil d'actualité des offres (query string)
    public class OfferQueryRequest
    {
        public int? DomainId { get; set; }
        public int? ProfessionId { get; set; }
        public string? ContractType { get; set; }
        public string? Search { get; set; }
        public string? City { get; set; }
        public int? RecruiterProfileId { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 12;
    }
}
