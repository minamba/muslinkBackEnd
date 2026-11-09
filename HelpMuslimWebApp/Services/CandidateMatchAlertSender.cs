using HelpMuslimWebApp.Domain.Models;
using HelpMuslimWebApp.Domain.Services;

namespace HelpMuslimWebApp.Api.Services
{
    // Alerte quotidienne aux recruteurs (8 h, heure de Paris) : nouveaux candidats qui correspondent à l'une de leurs offres
    // ouvertes à 70 % au moins. Chaque couple offre / candidat n'est signalé qu'une fois ; un candidat qui a déjà postulé
    // à l'offre n'est pas signalé. Recruteur Pro activé : le recruteur gratuit reçoit l'alerte sans information sur les candidats.
    // MatchAlerts:Hour change l'heure d'envoi ; MatchAlerts:RunOnStart envoie aussi au démarrage (tests).
    public class CandidateMatchAlertSender : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<CandidateMatchAlertSender> _logger;
        private readonly int _hour;
        private readonly bool _runOnStart;
        private readonly SemaphoreSlim _running = new(1, 1);

        public CandidateMatchAlertSender(IServiceScopeFactory scopeFactory, IConfiguration configuration, ILogger<CandidateMatchAlertSender> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _hour = Math.Clamp(configuration.GetValue("MatchAlerts:Hour", 8), 0, 23);
            _runOnStart = configuration.GetValue("MatchAlerts:RunOnStart", false);
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
                _logger.LogError(ex, "Alerte quotidienne des candidats impossible.");
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
                var services = scope.ServiceProvider;
                var alerts = services.GetRequiredService<IMatchAlertService>();
                var recruiters = services.GetRequiredService<IRecruiterProfileService>();
                var email = services.GetRequiredService<IEmailService>();
                var settings = await services.GetRequiredService<IPremiumService>().GetSettingsAsync();

                var offers = (await services.GetRequiredService<IJobOfferService>().GetJobOffersAsync(new OfferFilter { OnlyOpen = true, IncludeTestAccounts = true, PageSize = 10_000 }))
                    .Items.Where(o => o.RecruiterProfileId != null).ToList();
                if (offers.Count == 0) return 0;

                var alerted = await alerts.GetAlertedPairsAsync();
                var applied = await alerts.GetApplicationPairsAsync();
                var fresh = new List<(CandidateSummary Candidate, JobOffer Offer, int Score)>();
                foreach (var candidate in await services.GetRequiredService<ICandidateProfileService>().GetCandidatesForMatchingAsync())
                {
                    foreach (var match in CandidateMatcher.RankOffers(candidate, offers))
                    {
                        if (match.Score < MatchAlerts.Threshold) break;
                        if (candidate.IsTestAccount && !match.Offer.RecruiterIsTestAccount) continue;
                        if (!alerted.Contains((match.Offer.Id, candidate.Id))) fresh.Add((candidate, match.Offer, match.Score));
                    }
                }
                if (fresh.Count == 0) return 0;

                var sent = 0;
                foreach (var group in fresh.GroupBy(f => f.Offer.RecruiterProfileId!.Value))
                {
                    // Un candidat qui a déjà postulé à l'offre : le recruteur le connaît, le couple est seulement mémorisé
                    var toReport = group.Where(f => !applied.Contains((f.Offer.Id, f.Candidate.Id)))
                        .GroupBy(f => f.Candidate.Id)
                        .Select(c => c.OrderByDescending(f => f.Score).First())
                        .OrderByDescending(f => f.Score)
                        .ToList();
                    if (toReport.Count == 0) continue;

                    var recruiter = await recruiters.GetRecruiterProfileByIdAsync(group.Key);
                    if (recruiter == null || recruiter.IsDisabled || !recruiter.MatchAlertsEnabled || string.IsNullOrWhiteSpace(recruiter.Email)) continue;

                    var restricted = settings.RecruitersEnabled && !Premium.IsActive(recruiter.ProUntil);
                    var items = toReport.Take(MatchAlerts.MaxListed)
                        .Select(f => new CandidateMatchAlertItem(f.Candidate.Id, $"{f.Candidate.FirstName} {f.Candidate.LastName}".Trim(),
                            f.Candidate.Headline, f.Candidate.City, f.Offer.Title ?? string.Empty, f.Score))
                        .ToList();
                    await email.SendCandidateMatchesEmailAsync(recruiter.Email, recruiter.FirstName?.Trim() ?? string.Empty, items, toReport.Count, restricted,
                        await alerts.GetOrCreateTokenAsync(recruiter.Id));
                    sent++;
                }

                // Tous les nouveaux couples sont mémorisés, y compris ceux d'un recruteur sans alerte :
                // les réactiver plus tard n'envoie pas d'un coup les anciens candidats
                await alerts.AddAlertsAsync(fresh.Select(f => new MatchAlertPair(f.Offer.Id, f.Candidate.Id, f.Score)).ToList());
                _logger.LogInformation("Alerte candidats : {Pairs} nouvelle(s) correspondance(s), {Emails} email(s) envoyé(s).", fresh.Count, sent);
                return sent;
            }
            finally
            {
                _running.Release();
            }
        }
    }
}
