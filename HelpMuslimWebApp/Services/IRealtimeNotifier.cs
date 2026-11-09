using HelpMuslimWebApp.Api.ViewModels;

namespace HelpMuslimWebApp.Api.Services
{
    public interface IRealtimeNotifier
    {
        Task MessageReceivedAsync(string userId, MessageViewModel message);
        Task UnreadCountChangedAsync(string userId, int count);
        // Message modifié ou supprimé par son auteur : nouvel état chez les deux participants
        Task MessageUpdatedAsync(string userId, MessageViewModel message);

        // Candidat : une entreprise qu'il suit vient de publier une offre
        Task CompanyOfferPublishedAsync(string userId, CompanyOfferNotificationViewModel notification);

        // Bandeau "en direct" de l'accueil, diffusé à tous les visiteurs
        Task OfferFilledAsync(FilledOffersViewModel stats);
    }
}
