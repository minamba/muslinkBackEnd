namespace HelpMuslimWebApp.Domain.Models
{
    // Offres pourvues grâce à la plateforme : total cumulatif et dernières en date (bandeau "en direct" de l'accueil)
    public class FilledOffersStats
    {
        public int Count { get; set; }
        public List<FilledOffer> Recent { get; set; } = [];
    }

    public class FilledOffer
    {
        public int? JobOfferId { get; set; }
        public string? Title { get; set; }
        public string? CompanyName { get; set; }
        public string? CompanyLogoPath { get; set; }
        public string? City { get; set; }
        public string? ContractType { get; set; }
        public DateTime? FilledAt { get; set; }
    }
}
