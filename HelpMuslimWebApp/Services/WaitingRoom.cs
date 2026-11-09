using HelpMuslimWebApp.Api.Services.Demo;
using HelpMuslimWebApp.Dal.Entities;
using Microsoft.EntityFrameworkCore;

namespace HelpMuslimWebApp.Api.Services
{
    // Réponse au navigateur : il entre, ou il attend avec sa place dans la file. RetryAfter : prochain signe de vie (s)
    public record WaitingRoomTicket(bool Admitted, int? Position, int Waiting, int RetryAfter);

    // Administration › Modes : réglage et compteurs en direct
    public record WaitingRoomStatus(bool Enabled, int MaxVisitors, int Online, int Waiting, DateTime? UpdatedAt, string? UpdatedBy);

    // Salle d'attente : chaque navigateur signale sa présence (POST /waiting-room/ping) toutes les 30 s, toutes les 5 s
    // quand il attend. Sans signe de vie pendant 2 min 30 (onglet fermé), sa place se libère ; 45 s pour une place
    // dans la file. Salle ouverte : au-delà de MaxVisitors présents, les nouveaux venus prennent un numéro et entrent
    // dans l'ordre d'arrivée ; ceux déjà sur le site gardent leur place. Coupée, elle compte seulement les présents.
    // État en mémoire (une seule instance de l'Api) : un redémarrage vide la file, chacun reprend un numéro.
    public class WaitingRoom
    {
        public const int MinVisitors = 10;
        public const int MaxVisitorsLimit = 100_000;

        private const int AdmittedRetry = 30;
        private const int WaitingRetry = 5;
        private static readonly TimeSpan PresentFor = TimeSpan.FromSeconds(150);
        private static readonly TimeSpan WaitingFor = TimeSpan.FromSeconds(45);
        private static readonly TimeSpan SettingsFor = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan PurgeEvery = TimeSpan.FromSeconds(5);
        private static readonly WaitingRoomTicket Admitted = new(true, null, 0, AdmittedRetry);

        private readonly DemoDatabase _database;
        private readonly ILogger<WaitingRoom> _logger;
        private readonly object _lock = new();
        private readonly Dictionary<string, DateTime> _present = [];
        private readonly LinkedList<string> _queue = new();
        private readonly Dictionary<string, (LinkedListNode<string> Node, DateTime SeenAt)> _waiting = [];
        private volatile WaitingRoomSetting _settings = new() { Id = 1, MaxVisitors = 300 };
        private DateTime _settingsAt = DateTime.MinValue;
        private DateTime _purgedAt = DateTime.MinValue;

        public WaitingRoom(DemoDatabase database, ILogger<WaitingRoom> logger)
        {
            _database = database;
            _logger = logger;
        }

        public async Task<WaitingRoomTicket> PingAsync(string visitor)
        {
            var settings = await SettingsAsync();
            var now = DateTime.UtcNow;
            lock (_lock)
            {
                Purge(now);
                if (!settings.Enabled || _present.ContainsKey(visitor)) return Admit(visitor, now);

                // Places libres : servies aux premiers de la file, puis aux nouveaux venus s'il en reste
                var free = settings.MaxVisitors - _present.Count;
                if (!_waiting.TryGetValue(visitor, out var entry))
                {
                    if (free > _queue.Count) return Admit(visitor, now);
                    entry = (_queue.AddLast(visitor), now);
                }

                var position = PositionOf(entry.Node);
                if (position <= free)
                {
                    _queue.Remove(entry.Node);
                    _waiting.Remove(visitor);
                    return Admit(visitor, now);
                }
                _waiting[visitor] = (entry.Node, now);
                return new WaitingRoomTicket(false, position, _queue.Count, WaitingRetry);
            }
        }

        public async Task<WaitingRoomStatus> StatusAsync()
        {
            var settings = await SettingsAsync();
            lock (_lock)
            {
                Purge(DateTime.UtcNow, force: true);
                return new WaitingRoomStatus(settings.Enabled, settings.MaxVisitors, _present.Count, _queue.Count, settings.UpdatedAt, settings.UpdatedBy);
            }
        }

        public async Task<WaitingRoomStatus> UpdateAsync(bool enabled, int maxVisitors, string? updatedBy)
        {
            await using (var db = _database.CreateMainContext())
            {
                var row = await db.WaitingRoomSettings.FindAsync(1);
                if (row == null) db.WaitingRoomSettings.Add(row = new WaitingRoomSetting { Id = 1 });
                row.Enabled = enabled;
                row.MaxVisitors = maxVisitors;
                row.UpdatedAt = DateTime.UtcNow;
                row.UpdatedBy = updatedBy;
                await db.SaveChangesAsync();
                _settings = row;
                _settingsAt = DateTime.UtcNow;
            }
            return await StatusAsync();
        }

        // Réglage relu en base toutes les 30 s (toujours la vraie base, même pendant une requête de démo).
        // Base injoignable : dernier réglage connu
        private async Task<WaitingRoomSetting> SettingsAsync()
        {
            if (DateTime.UtcNow - _settingsAt < SettingsFor) return _settings;
            try
            {
                await using var db = _database.CreateMainContext();
                _settings = await db.WaitingRoomSettings.AsNoTracking().FirstOrDefaultAsync(s => s.Id == 1) ?? _settings;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Réglage de la salle d'attente illisible : dernier réglage connu conservé.");
            }
            _settingsAt = DateTime.UtcNow;
            return _settings;
        }

        private WaitingRoomTicket Admit(string visitor, DateTime now)
        {
            _present[visitor] = now;
            return Admitted;
        }

        private int PositionOf(LinkedListNode<string> node)
        {
            var position = 1;
            for (var current = _queue.First; current != null && current != node; current = current.Next) position++;
            return position;
        }

        // Navigateurs sans signe de vie : place sur le site ou dans la file libérée
        private void Purge(DateTime now, bool force = false)
        {
            if (!force && now - _purgedAt < PurgeEvery) return;
            _purgedAt = now;
            foreach (var visitor in _present.Where(p => now - p.Value > PresentFor).Select(p => p.Key).ToList())
                _present.Remove(visitor);
            foreach (var (visitor, entry) in _waiting.Where(w => now - w.Value.SeenAt > WaitingFor).ToList())
            {
                _queue.Remove(entry.Node);
                _waiting.Remove(visitor);
            }
        }
    }
}
