using HelpMuslimWebApp.Domain.Models;
using HelpMuslimWebApp.Domain.Services;

namespace HelpMuslimWebApp.Api.Services
{
    // Récapitulatif quotidien (8 h, heure de Paris) des nouvelles offres des entreprises suivies, pour les candidats gratuits quand
    // le mode premium est actif (les abonnés premium reçoivent un email à chaque offre). Un email par candidat au plus, et aucun
    // s'il ne reste rien de pertinent : offre fermée, pourvue ou masquée, entreprise plus suivie, ou candidature déjà envoyée.
    // Chaque offre n'est traitée qu'une fois. CompanyDigest:Hour change l'heure ; CompanyDigest:RunOnStart envoie aussi au démarrage (tests).
    public class CompanyOfferDigestSender : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<CompanyOfferDigestSender> _logger;
        private readonly int _hour;
        private readonly bool _runOnStart;
        private readonly SemaphoreSlim _running = new(1, 1);

        public CompanyOfferDigestSender(IServiceScopeFactory scopeFactory, IConfiguration configuration, ILogger<CompanyOfferDigestSender> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _hour = Math.Clamp(configuration.GetValue("CompanyDigest:Hour", 8), 0, 23);
            _runOnStart = configuration.GetValue("CompanyDigest:RunOnStart", false);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                if (_runOnStart) await RunSafelyAsync();
                while (!stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(DelayUntilNextRun(), stoppingToken);
                    await RunSafelyAsync();
                }
            }
            catch (OperationCanceledException)
            {
                // Arrêt de l'Api
            }
        }

        private async Task RunSafelyAsync()
        {
            try
            {
                await SendAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Récapitulatif des entreprises suivies impossible.");
            }
        }

        private TimeSpan DelayUntilNextRun()
        {
            TimeZoneInfo zone;
            try { zone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Paris"); }
            catch (TimeZoneNotFoundException) { zone = TimeZoneInfo.Utc; }

            var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone);
            var next = now.Date.AddHours(_hour);
            if (next <= now) next = next.AddDays(1);
            var delay = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(next, DateTimeKind.Unspecified), zone) - DateTime.UtcNow;
            return delay > TimeSpan.Zero ? delay : TimeSpan.FromMinutes(1);
        }

        // Retourne le nombre d'emails envoyés
        public async Task<int> SendAsync()
        {
            await _running.WaitAsync();
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var digests = scope.ServiceProvider.GetRequiredService<ICompanyDigestService>();
                var email = scope.ServiceProvider.GetRequiredService<IEmailService>();

                var pending = await digests.GetPendingAsync();
                if (pending.Count == 0) return 0;

                var sent = 0;
                foreach (var group in pending.GroupBy(p => p.CandidateProfileId))
                {
                    var first = group.First();
                    var offers = group.Where(p => p.OfferStillOpen && p.StillFollowing && !p.AlreadyApplied)
                        .DistinctBy(p => p.JobOfferId)
                        .Select(p => new CompanyDigestOffer(p.JobOfferId, p.OfferTitle ?? string.Empty, p.CompanyName ?? "Une entreprise",
                            string.Join(" · ", new[] { ContractTypes.Label(p.ContractType), p.City }.Where(s => !string.IsNullOrWhiteSpace(s)))))
                        .ToList();
                    try
                    {
                        if (offers.Count > 0 && !string.IsNullOrWhiteSpace(first.CandidateEmail))
                        {
                            await email.SendCompanyOffersDigestEmailAsync(first.CandidateEmail, first.CandidateFirstName?.Trim() ?? string.Empty, offers);
                            sent++;
                        }
                        // Traitées, envoyées ou non : jamais renvoyées
                        await digests.MarkSentAsync(group.Select(p => p.Id).ToList());
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Récapitulatif des entreprises suivies du candidat {CandidateId} impossible.", group.Key);
                    }
                }

                _logger.LogInformation("Récapitulatif entreprises suivies : {Pending} offre(s) en attente, {Emails} email(s) envoyé(s).", pending.Count, sent);
                return sent;
            }
            finally
            {
                _running.Release();
            }
        }
    }
}
