using System.Text.RegularExpressions;
using HelpMuslimWebApp.Api.Services;
using HelpMuslimWebApp.Api.Services.Demo;
using HelpMuslimWebApp.Api.Utils;
using HelpMuslimWebApp.Domain.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Swashbuckle.AspNetCore.Annotations;

namespace HelpMuslimWebApp.Api.Controllers
{
    // Identifiant tiré au sort par le navigateur (le même que pour le compteur de visites)
    public record WaitingRoomPing(string? Visitor);

    public record WaitingRoomUpdate(bool Enabled, int MaxVisitors);

    // Salle d'attente en cas de forte affluence (Administration › Modes)
    [ApiController]
    public partial class WaitingRoomController : ControllerBase
    {
        public const string RateLimitPolicy = "waiting-room";

        private readonly WaitingRoom _room;
        private readonly IDemoContext _demo;

        public WaitingRoomController(WaitingRoom room, IDemoContext demo)
        {
            _room = room ?? throw new ArgumentNullException(nameof(room));
            _demo = demo ?? throw new ArgumentNullException(nameof(demo));
        }

        // POST /waiting-room/ping { visitor } → entrer, ou attendre son tour (place dans la file)
        [HttpPost("waiting-room/ping")]
        [AllowAnonymous]
        [EnableRateLimiting(RateLimitPolicy)]
        [SwaggerResponse(StatusCodes.Status200OK, "Entrée ou place dans la file", typeof(WaitingRoomTicket))]
        public async Task<IActionResult> Ping([FromBody] WaitingRoomPing request)
        {
            // Administrateurs et visiteurs de la démo : jamais en attente, pas comptés
            if (User.IsAdministrator() || _demo.IsDemo) return Ok(new WaitingRoomTicket(true, null, 0, 30));
            if (request?.Visitor is not { } visitor || !VisitorRegex().IsMatch(visitor))
                return BadRequest(new { message = "Identifiant de visiteur invalide." });
            return Ok(await _room.PingAsync(visitor));
        }

        [HttpGet("admin/waiting-room")]
        [Authorize(Policy = Permissions.Modes.View)]
        [SwaggerResponse(StatusCodes.Status200OK, "Réglage et compteurs en direct", typeof(WaitingRoomStatus))]
        public async Task<IActionResult> Get() => Ok(await _room.StatusAsync());

        [HttpPut("admin/waiting-room")]
        [Authorize(Policy = Permissions.Modes.Edit)]
        [SwaggerResponse(StatusCodes.Status200OK, "Réglage enregistré", typeof(WaitingRoomStatus))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Nombre maximum hors limites")]
        public async Task<IActionResult> Update([FromBody] WaitingRoomUpdate model)
        {
            if (model.MaxVisitors < WaitingRoom.MinVisitors || model.MaxVisitors > WaitingRoom.MaxVisitorsLimit)
                return BadRequest(new { message = $"Le nombre maximum doit être compris entre {WaitingRoom.MinVisitors} et {WaitingRoom.MaxVisitorsLimit:N0}." });
            var name = string.Join(' ', new[] { User.GetFirstName(), User.GetLastName() }.Where(n => !string.IsNullOrWhiteSpace(n)));
            return Ok(await _room.UpdateAsync(model.Enabled, model.MaxVisitors, string.IsNullOrEmpty(name) ? User.GetEmail() : name));
        }

        [GeneratedRegex("^[A-Za-z0-9-]{8,64}$")]
        private static partial Regex VisitorRegex();
    }
}
