using HelpMuslimWebApp.Api.Utils;
using HelpMuslimWebApp.Api.Services.Coach;
using HelpMuslimWebApp.Api.Services.Telegram;
using HelpMuslimWebApp.Domain.Models;
using HelpMuslimWebApp.Domain.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace HelpMuslimWebApp.Api.Controllers
{
    // Administration › Anthropic (super admin) : la connexion du coach IA et de l'assistant des recruteurs fonctionne-t-elle encore ?
    [ApiController]
    [Route("admin/anthropic")]
    [Authorize]
    public class AdminAnthropicController : ControllerBase
    {
        private readonly AnthropicHealth _health;
        private readonly CvCoachClient _coach;
        private readonly TelegramQueue _telegram;

        public AdminAnthropicController(AnthropicHealth health, CvCoachClient coach, TelegramQueue telegram)
        {
            _health = health;
            _coach = coach;
            _telegram = telegram;
        }

        private bool CanView => User.HasPermission(Permissions.Anthropic.View);

        [HttpGet]
        [SwaggerResponse(StatusCodes.Status200OK, "État de la connexion et tarifs du coach")]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "Réservé au super admin")]
        public IActionResult Get() => CanView ? Ok(View(_health.Current)) : Forbid();

        // Vérifie tout de suite (un message minuscule, une fraction de centime)
        [HttpPost("check")]
        [SwaggerResponse(StatusCodes.Status200OK, "État après vérification")]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "Réservé au super admin")]
        public async Task<IActionResult> Check(CancellationToken cancellationToken)
        {
            if (!CanView) return Forbid();
            return Ok(View(await _health.CheckAsync(cancellationToken)));
        }

        private object View(AnthropicHealthState state)
        {
            var (input, output) = CoachPricing.PriceOf(_coach.Model);
            return new
            {
                state.Status,
                state.CheckedAt,
                state.Since,
                state.DurationMs,
                state.Detail,
                CheckModel = AnthropicHealth.CheckModel,
                CoachModel = _coach.Model,
                CoachInputUsdPerMillion = input,
                CoachOutputUsdPerMillion = output,
                IntervalMinutes = (int)AnthropicHealth.Interval.TotalMinutes,
                DownIntervalMinutes = (int)AnthropicHealth.DownInterval.TotalMinutes,
                // Coupure et rétablissement signalés dans le groupe Telegram « mail »
                TelegramAlerts = _telegram.IsConfigured(TelegramBot.ContactMessage),
            };
        }
    }
}
