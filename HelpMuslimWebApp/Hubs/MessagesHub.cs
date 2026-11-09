using HelpMuslimWebApp.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace HelpMuslimWebApp.Api.Hubs
{
    // Canal temps réel de la messagerie. Les messages sont envoyés via l'Api REST ;
    // le hub pousse les événements aux clients connectés :
    //   "messageReceived"     (MessageViewModel)
    //   "messageUpdated"      (MessageViewModel : message modifié ou supprimé par son auteur)
    //   "unreadCountChanged"  (int)
    // et reçoit la conversation affichée à l'écran (SetPresence) : pas d'email à qui lit déjà la discussion.
    [Authorize]
    public class MessagesHub : Hub
    {
        private readonly ConversationPresence _presence;

        public MessagesHub(ConversationPresence presence)
        {
            _presence = presence;
        }

        // conversationId : conversation ouverte (null : aucune) ; visible : onglet au premier plan
        public void SetPresence(int? conversationId, bool visible)
        {
            if (Context.UserIdentifier is { } userId) _presence.Set(Context.ConnectionId, userId, conversationId, visible);
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            _presence.Remove(Context.ConnectionId);
            return base.OnDisconnectedAsync(exception);
        }
    }

    // Identifie les connexions par le claim "sub" (id utilisateur IdentityServer)
    public class SubjectUserIdProvider : IUserIdProvider
    {
        public string? GetUserId(HubConnectionContext connection) => connection.User?.FindFirst("sub")?.Value;
    }
}
