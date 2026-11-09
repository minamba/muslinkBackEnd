using System.Security.Claims;
using HelpMuslimWebApp.Api.Request;
using HelpMuslimWebApp.Api.ViewModels;

namespace HelpMuslimWebApp.Api.Builders
{
    public interface IConversationViewModelBuilder
    {
        Task<IEnumerable<ConversationViewModel>> GetMyConversationsAsync(ClaimsPrincipal user);
        Task<ConversationViewModel?> GetConversationAsync(ClaimsPrincipal user, int id);
        Task<IEnumerable<MessageViewModel>?> GetMessagesAsync(ClaimsPrincipal user, int conversationId);
        Task<MessageViewModel?> SendMessageAsync(ClaimsPrincipal user, int conversationId, MessageRequest model);
        // Son propre message écrit (null : conversation ou message introuvable)
        Task<MessageViewModel?> EditMessageAsync(ClaimsPrincipal user, int conversationId, int messageId, MessageRequest model);
        // Son propre message, remplacé par « Message supprimé » (null : conversation ou message introuvable)
        Task<MessageViewModel?> DeleteOwnMessageAsync(ClaimsPrincipal user, int conversationId, int messageId);
        // Partages : numéro du profil, fiche de poste (recruteur), CV (candidat) ; null = conversation introuvable
        Task<MessageViewModel?> SharePhoneAsync(ClaimsPrincipal user, int conversationId);
        Task<MessageViewModel?> ShareOfferAsync(ClaimsPrincipal user, int conversationId, int offerId);
        Task<MessageViewModel?> ShareCvAsync(ClaimsPrincipal user, int conversationId);
        Task<(Stream Stream, string FileName, string ContentType)?> GetMessageCvAsync(ClaimsPrincipal user, int conversationId, int messageId);
        Task<ConversationViewModel?> StartConversationAsync(ClaimsPrincipal user, StartConversationRequest model);  // null = candidat introuvable
        Task<int> GetUnreadCountAsync(ClaimsPrincipal user);

        // Administration
        Task<IEnumerable<ConversationViewModel>> GetAllConversationsAsync();
        Task<IEnumerable<MessageViewModel>?> GetConversationMessagesAsync(int conversationId);
        Task<bool> DeleteMessageAsync(int messageId);
    }
}
