using HelpMuslimWebApp.Api.Utils;
using HelpMuslimWebApp.Domain.Models;
using HelpMuslimWebApp.Domain.Security;
using HelpMuslimWebApp.Domain.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace HelpMuslimWebApp.Api.Controllers
{
    // Tableau de bord : Fréquentation (visiteurs, pays, inscriptions et candidatures), Abonnements, Frais et Gains d'une période.
    // Un droit par onglet (le super admin les a tous)
    [ApiController]
    [Route("admin/stats")]
    [Authorize]
    public class AdminStatsController : Controller
    {
        private readonly ISiteStatsService _stats;
        private readonly IPremiumService _premiumService;
        private readonly TimeZoneInfo _timeZone;
        // Conversion des frais en euros : taux fixe (Costs:EurPerUsd), c'est un ordre de grandeur, pas de la comptabilité
        private readonly decimal _eurPerUsd;

        public AdminStatsController(ISiteStatsService stats, IPremiumService premiumService, IConfiguration config)
        {
            _stats = stats ?? throw new ArgumentNullException(nameof(stats));
            _premiumService = premiumService ?? throw new ArgumentNullException(nameof(premiumService));
            _timeZone = SiteTimeZone(config["Stats:TimeZone"]);
            _eurPerUsd = config.GetValue("Costs:EurPerUsd", 0.92m);
        }

        // GET /admin/stats/traffic?scale=day|week|month|year&date=2026-10-28 → la période qui contient cette date
        [HttpGet("traffic")]
        [Authorize(Policy = Permissions.Stats.View)]
        [SwaggerResponse(StatusCodes.Status200OK, "Fréquentation de la période", typeof(SiteStats))]
        public async Task<IActionResult> Traffic([FromQuery] string? scale, [FromQuery] DateOnly? date) =>
            Ok(await _stats.GetStatsAsync(ParseScale(scale), date ?? Today, _timeZone));

        // GET /admin/stats/subscriptions?scale=…&date=… → abonnements candidats et recruteurs de la période.
        // Noms des abonnés dans les derniers mouvements : réservés à qui gère le premium
        [HttpGet("subscriptions")]
        [Authorize(Policy = Permissions.Dashboard.Subscriptions)]
        [SwaggerResponse(StatusCodes.Status200OK, "Abonnements de la période", typeof(SubscriptionStats))]
        public async Task<IActionResult> Subscriptions([FromQuery] string? scale, [FromQuery] DateOnly? date)
        {
            var stats = await _stats.GetSubscriptionStatsAsync(ParseScale(scale), date ?? Today, _timeZone);
            if (!User.HasPermission(Permissions.Premium.Manage))
            {
                stats.Candidates.Latest = stats.Candidates.Latest.Select(m => m with { Name = null }).ToList();
                stats.Recruiters.Latest = stats.Recruiters.Latest.Select(m => m with { Name = null }).ToList();
            }
            return Ok(stats);
        }

        // GET /admin/stats/coach-costs?scale=…&date=… → ce que le coach IA a coûté, par candidat
        [HttpGet("coach-costs")]
        [Authorize(Policy = Permissions.Dashboard.Costs)]
        [SwaggerResponse(StatusCodes.Status200OK, "Frais du coach IA sur la période", typeof(CoachCostStats))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "Droit manquant")]
        public async Task<IActionResult> CoachCosts([FromQuery] string? scale, [FromQuery] DateOnly? date)
        {
            var stats = await _stats.GetCoachCostStatsAsync(ParseScale(scale), date ?? Today, _timeZone);
            stats.EurPerUsd = _eurPerUsd;
            return Ok(stats);
        }

        // GET /admin/stats/recruiter-ai-costs?scale=…&date=… → ce que l'assistant IA des recruteurs a coûté, par recruteur
        [HttpGet("recruiter-ai-costs")]
        [Authorize(Policy = Permissions.Dashboard.Costs)]
        [SwaggerResponse(StatusCodes.Status200OK, "Frais de l'assistant IA des recruteurs sur la période", typeof(RecruiterAiCostStats))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "Droit manquant")]
        public async Task<IActionResult> RecruiterAiCosts([FromQuery] string? scale, [FromQuery] DateOnly? date)
        {
            var stats = await _stats.GetRecruiterAiCostStatsAsync(ParseScale(scale), date ?? Today, _timeZone);
            stats.EurPerUsd = _eurPerUsd;
            return Ok(stats);
        }

        // GET /admin/stats/revenue?scale=month|year&date=… → ce que rapportent les abonnements
        [HttpGet("revenue")]
        [Authorize(Policy = Permissions.Dashboard.Revenue)]
        [SwaggerResponse(StatusCodes.Status200OK, "Gains des abonnements sur le mois ou l'année", typeof(RevenueStats))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "Droit manquant")]
        public async Task<IActionResult> Revenue([FromQuery] string? scale, [FromQuery] DateOnly? date)
        {
            var prices = await _premiumService.GetSettingsAsync();
            return Ok(await _stats.GetRevenueStatsAsync(ParseScale(scale), date ?? Today, _timeZone, prices));
        }

        private static StatsScale ParseScale(string? scale) => scale?.ToLowerInvariant() switch
        {
            "week" => StatsScale.Week,
            "month" => StatsScale.Month,
            "year" => StatsScale.Year,
            _ => StatsScale.Day,
        };

        private DateOnly Today => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _timeZone));

        // Heure du site (Europe/Paris par défaut) : « le 28 octobre » commence à minuit à Paris, pas à minuit UTC
        private static TimeZoneInfo SiteTimeZone(string? id)
        {
            foreach (var candidate in new[] { id, "Europe/Paris", "Romance Standard Time" })
            {
                if (!string.IsNullOrWhiteSpace(candidate) && TimeZoneInfo.TryFindSystemTimeZoneById(candidate, out var zone)) return zone;
            }
            return TimeZoneInfo.Utc;
        }
    }
}
