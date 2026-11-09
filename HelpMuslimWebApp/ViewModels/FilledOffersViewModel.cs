namespace HelpMuslimWebApp.Api.ViewModels
{
    // Bandeau "en direct" de l'accueil : offres pourvues grâce à Muslink
    public class FilledOffersViewModel
    {
        public int Count { get; set; }
        public List<FilledOfferViewModel> Recent { get; set; } = [];
    }

    public class FilledOfferViewModel
    {
        // Null si l'offre a été supprimée depuis
        public int? JobOfferId { get; set; }
        public string? Title { get; set; }
        public string? CompanyName { get; set; }
        public string? CompanyLogoUrl { get; set; }
        public string? City { get; set; }
        public string? ContractType { get; set; }
        public DateTime? FilledAt { get; set; }
    }
}
