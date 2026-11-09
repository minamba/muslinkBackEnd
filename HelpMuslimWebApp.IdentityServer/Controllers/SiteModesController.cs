using HelpMuslimWebApp.Domain.Security;
using HelpMuslimWebApp.IdentityServer.Models;
using HelpMuslimWebApp.IdentityServer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HelpMuslimWebApp.IdentityServer.Controllers
{
    // Modes du site : état public (front et Api) et onglet « Modes » de l'administration
    [ApiController]
    public class SiteModesController : ControllerBase
    {
        private readonly SiteModeService _siteModes;

        public SiteModesController(SiteModeService siteModes)
        {
            _siteModes = siteModes ?? throw new ArgumentNullException(nameof(siteModes));
        }

        // GET /api/site/modes → { privateAccess, maintenance } (public, jamais mis en cache)
        [HttpGet("api/site/modes")]
        [AllowAnonymous]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> GetPublicState()
        {
            var state = await _siteModes.GetStateAsync();
            return Ok(new { state.PrivateAccess, state.Maintenance, state.TestMode });
        }

        [HttpGet("api/admin/modes")]
        [Authorize(Policy = Permissions.Modes.View)]
        public async Task<IActionResult> GetModes() =>
            Ok((await _siteModes.GetAllAsync()).Select(m => ToViewModel(m.Definition, m.State)));

        // PUT /api/admin/modes/{key} { isEnabled }
        [HttpPut("api/admin/modes/{key}")]
        [Authorize(Policy = Permissions.Modes.Edit)]
        public async Task<IActionResult> SetMode(string key, [FromBody] SiteModeRequest request)
        {
            var definition = SiteModeCatalog.All.FirstOrDefault(d => d.Key == key);
            if (definition == null) return NotFound(new { message = "Mode inconnu." });
            if (key == SiteModeKeys.TestMode && !User.HasClaim("role", SystemRoles.SuperAdmin) && !User.HasClaim(Permissions.ClaimType, Permissions.Modes.Test))
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Activer ou couper le mode test demande le droit « Mode test »." });

            var updatedBy = User.FindFirst("name")?.Value ?? User.FindFirst("email")?.Value;
            var mode = await _siteModes.SetAsync(key, request.IsEnabled, updatedBy);
            return Ok(ToViewModel(definition, mode));
        }

        private static object ToViewModel(SiteModeDefinition definition, SiteMode? state) => new
        {
            definition.Key,
            definition.Label,
            definition.Description,
            definition.Effects,
            IsEnabled = state?.IsEnabled ?? false,
            UpdatedAt = state?.UpdatedAt,
            state?.UpdatedBy,
        };
    }

    public class SiteModeRequest
    {
        public bool IsEnabled { get; set; }
    }
}
