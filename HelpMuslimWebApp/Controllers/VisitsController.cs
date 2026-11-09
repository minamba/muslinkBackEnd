using System.Text.RegularExpressions;
using HelpMuslimWebApp.Api.Services;
using HelpMuslimWebApp.Domain.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HelpMuslimWebApp.Api.Controllers
{
    // Ce que le navigateur envoie en arrivant sur le site : l'identifiant qu'il s'est tiré au sort et rangé dans son
    // stockage local. Ni page, ni référent : l'heure est celle du serveur, le pays déduit de l'adresse au passage.
    // Source : provenance déduite par le navigateur (google, moteur, reseaux, agregateur, site, direct)
    public record VisitRequest(string? Visitor, string? Source = null);

    // Compteur de visites du site public (onglet Fréquentation du tableau de bord).
    // Ouvert à tous : c'est tout l'objet, compter aussi ceux qui ne sont pas encore inscrits.
    [ApiController]
    [AllowAnonymous]
    [Route("visits")]
    public partial class VisitsController : ControllerBase
    {
        public const string RateLimitPolicy = "visits";

        private readonly ISiteStatsService _stats;
        private readonly GeoCountryResolver _geo;
        private readonly ILogger<VisitsController> _logger;

        public VisitsController(ISiteStatsService stats, GeoCountryResolver geo, ILogger<VisitsController> logger)
        {
            _stats = stats ?? throw new ArgumentNullException(nameof(stats));
            _geo = geo ?? throw new ArgumentNullException(nameof(geo));
            _logger = logger;
        }

        // POST /visits { visitor } → toujours 204 : un compteur ne fait jamais apparaître d'erreur chez un visiteur
        [HttpPost]
        [EnableRateLimiting(RateLimitPolicy)]
        public async Task<IActionResult> Record([FromBody] VisitRequest request)
        {
            // Robots d'indexation et navigateurs automatisés : pas des visiteurs
            if (BotRegex().IsMatch(Request.Headers.UserAgent.ToString())) return NoContent();

            try
            {
                await _stats.RecordVisitAsync(request?.Visitor, _geo.CountryOf(HttpContext.Connection.RemoteIpAddress), request?.Source);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Visite non enregistrée.");
            }
            return NoContent();
        }

        [GeneratedRegex(@"bot|crawl|spider|slurp|headless|lighthouse|preview|facebookexternalhit|whatsapp|python|curl|wget|^$", RegexOptions.IgnoreCase)]
        private static partial Regex BotRegex();
    }
}
