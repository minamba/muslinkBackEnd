using HelpMuslimWebApp.Api.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace HelpMuslimWebApp.Api.Services.Mail
{
    // Nouveaux emails de la boîte support → notification en bas à droite de l'administration.
    // Une relève par minute, seulement quand un administrateur a un onglet ouvert.
    // « Nouveau » = non lu, d'identifiant IMAP plus grand que le plus grand déjà vu. La première relève (ou celle qui
    // suit une absence de plus de 3 minutes) ne fait que prendre ce repère : un administrateur qui revient le matin
    // ne reçoit pas d'un coup les emails de la nuit, qu'il verra en ouvrant la boîte.
    public class SupportMailWatcher : BackgroundService
    {
        private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);
        private static readonly TimeSpan Absence = TimeSpan.FromMinutes(3);

        private readonly ISupportMailbox _mailbox;
        private readonly AdminMailPresence _presence;
        private readonly IHubContext<AdminMailHub> _hub;
        private readonly ILogger<SupportMailWatcher> _logger;
        private (uint LastUid, DateTime SeenAt)? _marker;

        public SupportMailWatcher(ISupportMailbox mailbox, AdminMailPresence presence, IHubContext<AdminMailHub> hub, ILogger<SupportMailWatcher> logger)
        {
            _mailbox = mailbox;
            _presence = presence;
            _hub = hub;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_mailbox.IsConfigured) return;

            using var timer = new PeriodicTimer(Interval);
            do
            {
                try
                {
                    if (_presence.AnyoneConnected) await CheckAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Boîte support : relève impossible.");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }

        private async Task CheckAsync(CancellationToken ct)
        {
            var result = await _mailbox.ListAsync(MailFolder.Inbox, 1, 10, ct);
            if (result.Error != null) return;

            var now = DateTime.UtcNow;
            var highest = result.Items.Count == 0 ? 0u : result.Items.Max(m => m.Uid);

            // Première relève, retour après une absence, ou boîte renumérotée par le serveur : on prend le repère
            if (_marker is not { } marker || now - marker.SeenAt > Absence || highest < marker.LastUid)
            {
                _marker = (highest, now);
                return;
            }

            var fresh = result.Items.Where(m => m.Uid > marker.LastUid && !m.IsRead).OrderBy(m => m.Uid).ToList();
            _marker = (Math.Max(highest, marker.LastUid), now);
            if (fresh.Count == 0) return;

            await _hub.Clients.All.SendAsync("mailReceived", new
            {
                unread = result.Unread,
                messages = fresh.Select(m => new { m.Uid, m.FromName, m.FromAddress, m.Subject, m.Preview }),
            }, ct);
        }
    }
}
