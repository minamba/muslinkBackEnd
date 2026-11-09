using System.Collections.Concurrent;
using HelpMuslimWebApp.Domain.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace HelpMuslimWebApp.Api.Hubs
{
    // Canal temps réel de la boîte support, réservé aux personnes qui peuvent la lire :
    //   "mailReceived"  ({ unread, messages: [{ uid, fromName, fromAddress, subject, preview }] })
    [Authorize(Policy = Permissions.Mails.View)]
    public class AdminMailHub : Hub
    {
        private readonly AdminMailPresence _presence;

        public AdminMailHub(AdminMailPresence presence)
        {
            _presence = presence;
        }

        public override Task OnConnectedAsync()
        {
            _presence.Add(Context.ConnectionId);
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            _presence.Remove(Context.ConnectionId);
            return base.OnDisconnectedAsync(exception);
        }
    }

    // Nombre d'onglets connectés : la boîte n'est relevée que si quelqu'un peut voir la notification
    public class AdminMailPresence
    {
        private readonly ConcurrentDictionary<string, byte> _connections = new();

        public bool AnyoneConnected => !_connections.IsEmpty;

        public void Add(string connectionId) => _connections.TryAdd(connectionId, 0);

        public void Remove(string connectionId) => _connections.TryRemove(connectionId, out _);
    }
}
