namespace HelpMuslimWebApp.Domain.Models
{
    // Vue réduite d'un candidat pour les listes (candidatures, favoris, admin)
    public class CandidateSummary
    {
        public int Id { get; set; }
        public string? UserId { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? Headline { get; set; }
        public string? City { get; set; }
        public string? Situation { get; set; }
        // Disponible à partir de cette date (aujourd'hui ou avant : tout de suite) ; vide si non renseigné
        public DateOnly? AvailableFrom { get; set; }
        public string? DesiredJob { get; set; }
        public string? PhotoPath { get; set; }
        public DateTime? CreatedAt { get; set; }

        // Renseignés pour le calcul de correspondance avec les offres
        public int? DesiredDomainId { get; set; }
        public int? DesiredProfessionId { get; set; }
        public string? DesiredProfessionName { get; set; }
        public List<string> Skills { get; set; } = [];
        // Intitulés des expériences et métiers souhaités : rapprochés de l'offre
        public List<string> ExperienceTitles { get; set; } = [];
        public List<int> WishProfessionIds { get; set; } = [];

        // Premium (mise en avant chez les recruteurs quand le mode premium est actif)
        public DateTime? PremiumGrantedUntil { get; set; }
        public DateTime? PremiumPaidUntil { get; set; }
        public DateTime? PremiumUntil => Premium.Until(PremiumGrantedUntil, PremiumPaidUntil);

        // Compte de test : invisible des recruteurs qui ne sont pas eux-mêmes des comptes de test
        public bool IsTestAccount { get; set; }
    }
}
