namespace HelpMuslimWebApp.Api.ViewModels
{
    public class CandidateSummaryViewModel
    {
        public int Id { get; set; }
        public string? UserId { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Headline { get; set; }
        public string? City { get; set; }
        public string? Situation { get; set; }
        // Disponible à partir de cette date (aujourd'hui ou avant : tout de suite) ; vide si non renseigné
        public DateOnly? AvailableFrom { get; set; }
        public string? DesiredJob { get; set; }
        public string? PhotoUrl { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string? DesiredProfessionName { get; set; }
        public List<string> Skills { get; set; } = [];

        // Vue recruteur, mode premium actif : candidat premium, mis en avant (badge « Premium »)
        public bool IsPremium { get; set; }
    }

    // Vue recruteur : candidat classé selon ses offres
    // Recruteur › Candidats : une offre ouverte et le nombre de candidats en lien avec elle
    public record OfferMatchCountViewModel(int OfferId, string Title, int Count);

    // Recruteur › Candidats : une ville et le nombre de candidats qui y vivent
    public record CityCountViewModel(string City, int Count);

    public class CandidateMatchViewModel
    {
        public CandidateSummaryViewModel? Candidate { get; set; }
        public int Score { get; set; }
        public int? OfferId { get; set; }
        public string? OfferTitle { get; set; }
        public List<string> Reasons { get; set; } = [];
        public bool IsFavorite { get; set; }

        // Recruteur gratuit (Recruteur Pro activé) : profil au-delà des premiers, verrouillé (Candidate et Reasons vides)
        public bool IsLocked { get; set; }
    }
}
