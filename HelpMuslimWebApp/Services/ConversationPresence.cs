using System.Collections.Concurrent;

namespace HelpMuslimWebApp.Api.Services
{
    // Qui a quelle conversation ouverte à l'écran, par connexion temps réel (un onglet = une connexion).
    // Sert à ne pas envoyer d'email d'alerte à quelqu'un qui lit déjà la discussion en direct.
    // Onglet passé en arrière-plan : toujours considéré comme présent pendant 2 minutes (un coup d'œil ailleurs).
    public class ConversationPresence
    {
        private static readonly TimeSpan HiddenGrace = TimeSpan.FromMinutes(2);

        private record Entry(string UserId, int? ConversationId, bool Visible, DateTime ChangedAt);

        private readonly ConcurrentDictionary<string, Entry> _connections = new();

        public void Set(string connectionId, string userId, int? conversationId, bool visible) =>
            _connections[connectionId] = new Entry(userId, conversationId, visible, DateTime.UtcNow);

        public void Remove(string connectionId) => _connections.TryRemove(connectionId, out _);

        public bool IsViewing(string userId, int conversationId)
        {
            var now = DateTime.UtcNow;
            return _connections.Values.Any(e => e.UserId == userId && e.ConversationId == conversationId
                && (e.Visible || now - e.ChangedAt < HiddenGrace));
        }
    }
}
