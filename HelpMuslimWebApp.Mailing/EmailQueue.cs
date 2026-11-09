using System.Threading.Channels;
using MailKit.Net.Smtp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HelpMuslimWebApp.Mailing
{
    // File d'envoi en arrière-plan : l'action en cours (inscription, candidature…) n'attend pas le serveur SMTP
    // et ne peut pas échouer à cause d'un email
    public interface IEmailQueue
    {
        void Enqueue(EmailMessage email);
    }

    // La file est gardée en base (table OutboxEmail du contexte TContext) : un email en attente survit au redémarrage
    // du container, et un refus du SMTP (blocage, quota du relais, panne) est réessayé pendant environ 24 h
    public class EmailQueue<TContext> : IEmailQueue where TContext : DbContext
    {
        private readonly IServiceScopeFactory _scopes;
        private readonly EmailQueueSignal _signal;
        private readonly ILogger<EmailQueue<TContext>> _logger;

        public EmailQueue(IServiceScopeFactory scopes, EmailQueueSignal signal, ILogger<EmailQueue<TContext>> logger)
        {
            _scopes = scopes;
            _signal = signal;
            _logger = logger;
        }

        public void Enqueue(EmailMessage email)
        {
            try
            {
                // Contexte à part : l'email est enregistré même si l'appelant n'enregistre pas ses propres changements
                using var scope = _scopes.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<TContext>();
                db.Set<OutboxEmail>().Add(OutboxEmail.From(email, DateTime.UtcNow));
                db.SaveChanges();
                _signal.Wake();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email « {Subject} » à {To} non mis en file d'envoi.", email.Subject, email.To);
            }
        }
    }

    // Réveille le service d'envoi dès qu'un email arrive (sinon il regarde la file chaque minute)
    public class EmailQueueSignal
    {
        private readonly Channel<bool> _channel = Channel.CreateBounded<bool>(new BoundedChannelOptions(1) { FullMode = BoundedChannelFullMode.DropWrite });

        public void Wake() => _channel.Writer.TryWrite(true);

        internal async Task WaitAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            using var timer = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timer.CancelAfter(timeout);
            try
            {
                await _channel.Reader.ReadAsync(timer.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
            }
        }
    }

    public class EmailQueueWorker<TContext> : BackgroundService where TContext : DbContext
    {
        // Nouvel essai après 1 min, 5 min, 15 min, 30 min, 1 h, 2 h, 4 h, 8 h puis 8 h : abandon au bout d'environ 24 h,
        // de quoi passer un blocage du SMTP mutualisé ou le quota journalier du relais
        private static readonly TimeSpan[] RetryDelays =
        [
            TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(30),
            TimeSpan.FromHours(1), TimeSpan.FromHours(2), TimeSpan.FromHours(4), TimeSpan.FromHours(8), TimeSpan.FromHours(8),
        ];

        // Après un refus, pause avant l'email suivant : un serveur qui refuse n'est pas sollicité en boucle
        private static readonly TimeSpan FailurePause = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan Idle = TimeSpan.FromMinutes(1);
        private static readonly TimeSpan KeepAbandoned = TimeSpan.FromDays(30);

        private readonly IServiceScopeFactory _scopes;
        private readonly EmailQueueSignal _signal;
        private readonly IEmailSender _sender;
        private readonly ILogger<EmailQueueWorker<TContext>> _logger;

        public EmailQueueWorker(IServiceScopeFactory scopes, EmailQueueSignal signal, IEmailSender sender, ILogger<EmailQueueWorker<TContext>> logger)
        {
            _scopes = scopes;
            _signal = signal;
            _sender = sender;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await SendDueAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    // Base injoignable : nouvel essai au prochain réveil
                    _logger.LogError(ex, "File d'envoi des emails illisible.");
                }
                await _signal.WaitAsync(Idle, stoppingToken);
            }
        }

        // Envoie, du plus ancien au plus récent, les emails dont l'heure d'essai est passée
        private async Task SendDueAsync(CancellationToken stoppingToken)
        {
            using var scope = _scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TContext>();
            var outbox = db.Set<OutboxEmail>();

            var expired = DateTime.UtcNow - KeepAbandoned;
            await outbox.Where(e => e.FailedAt != null && e.FailedAt < expired).ExecuteDeleteAsync(stoppingToken);

            while (true)
            {
                var now = DateTime.UtcNow;
                var email = await outbox.Where(e => e.FailedAt == null && e.NextAttemptAt <= now).OrderBy(e => e.Id).FirstOrDefaultAsync(stoppingToken);
                if (email == null) return;

                Exception? error = null;
                try
                {
                    await _sender.SendAsync(email.ToMessage(), stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException || !stoppingToken.IsCancellationRequested)
                {
                    error = ex;
                }

                if (error == null)
                {
                    // Parti : retiré même si l'arrêt est demandé entre-temps (sinon il repartirait au redémarrage)
                    outbox.Remove(email);
                    await db.SaveChangesAsync(CancellationToken.None);
                    continue;
                }

                email.Attempts++;
                email.LastError = error.Message.Length > 1000 ? error.Message[..1000] : error.Message;
                var rejected = IsRecipientRejected(error);
                if (rejected || email.Attempts > RetryDelays.Length)
                {
                    email.FailedAt = DateTime.UtcNow;
                    _logger.LogError(error, "Email « {Subject} » à {To} abandonné après {Attempts} essai(s).", email.Subject, email.To, email.Attempts);
                }
                else
                {
                    var delay = RetryDelays[email.Attempts - 1];
                    email.NextAttemptAt = DateTime.UtcNow + delay;
                    _logger.LogWarning(error, "Envoi de « {Subject} » à {To} impossible, nouvel essai dans {Delay}.", email.Subject, email.To, delay);
                }
                await db.SaveChangesAsync(CancellationToken.None);

                if (!rejected) await Task.Delay(FailurePause, stoppingToken);
            }
        }

        // Adresse refusée définitivement (5xx) : inutile de réessayer
        private static bool IsRecipientRejected(Exception ex) =>
            ex is SmtpCommandException { ErrorCode: SmtpErrorCode.RecipientNotAccepted } command && (int)command.StatusCode >= 500;
    }

    public static class EmailServiceCollectionExtensions
    {
        // services.AddMuslinkEmail<Contexte>(builder.Configuration) : réglages, envoi direct, et file d'envoi gardée
        // dans la base du contexte (qui appelle modelBuilder.AddEmailOutbox())
        public static IServiceCollection AddMuslinkEmail<TContext>(this IServiceCollection services, IConfiguration configuration) where TContext : DbContext
        {
            services.Configure<EmailSettings>(settings =>
            {
                configuration.GetSection(EmailSettings.SectionName).Bind(settings);
                if (string.IsNullOrWhiteSpace(settings.SiteUrl))
                    settings.SiteUrl = configuration["FrontUrl"];
            });
            services.AddSingleton<IEmailLayout, EmailLayout>();
            services.AddSingleton<IEmailSender, EmailSender>();
            services.AddSingleton<EmailQueueSignal>();
            services.AddSingleton<IEmailQueue, EmailQueue<TContext>>();
            services.AddHostedService<EmailQueueWorker<TContext>>();
            return services;
        }
    }
}
