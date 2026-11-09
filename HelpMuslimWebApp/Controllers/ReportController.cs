using HelpMuslimWebApp.Api.Builders;
using HelpMuslimWebApp.Api.Request;
using HelpMuslimWebApp.Api.ViewModels;
using HelpMuslimWebApp.Domain.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace HelpMuslimWebApp.Api.Controllers
{
    // Signalements : faits par les candidats et les recruteurs, traités depuis l'administration
    [ApiController]
    [Route("reports")]
    [Authorize]
    public class ReportController : Controller
    {
        private readonly IReportViewModelBuilder _builder;

        public ReportController(IReportViewModelBuilder builder)
        {
            _builder = builder ?? throw new ArgumentNullException(nameof(builder));
        }

        [HttpPost]
        [SwaggerResponse(StatusCodes.Status200OK, "Signalement transmis à l'équipe")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Raison, justification ou élément invalide")]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "Signalement non autorisé pour ce compte")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Élément signalé introuvable")]
        [SwaggerResponse(StatusCodes.Status409Conflict, "Déjà signalé, ou trop de signalements aujourd'hui")]
        public async Task<IActionResult> Create([FromBody] ReportRequest request)
        {
            await _builder.CreateAsync(User, request);
            return Ok();
        }

        [HttpGet("admin")]
        [Authorize(Policy = Permissions.Reports.View)]
        [SwaggerResponse(StatusCodes.Status200OK, "Signalements, du plus récent au plus ancien", typeof(IEnumerable<ReportViewModel>))]
        public async Task<IActionResult> GetReports([FromQuery] string? type) => Ok(await _builder.GetReportsAsync(type));

        [HttpGet("admin/counts")]
        [Authorize(Policy = Permissions.Reports.View)]
        [SwaggerResponse(StatusCodes.Status200OK, "Signalements « nouveau » par type", typeof(Dictionary<string, int>))]
        public async Task<IActionResult> GetCounts() => Ok(await _builder.CountNewAsync());

        [HttpPatch("admin/{id:int}/status")]
        [Authorize(Policy = Permissions.Reports.Manage)]
        [SwaggerResponse(StatusCodes.Status200OK, "Statut changé, le signalant est prévenu par email", typeof(ReportViewModel))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Signalement introuvable")]
        public async Task<IActionResult> SetStatus(int id, [FromBody] ReportStatusRequest request) =>
            await _builder.SetStatusAsync(User, id, request) is { } report ? Ok(report) : NotFound();

        [HttpPut("admin/{id:int}")]
        [Authorize(Policy = Permissions.Reports.Manage)]
        [SwaggerResponse(StatusCodes.Status200OK, "Signalement corrigé (l'auteur n'est pas prévenu)", typeof(ReportViewModel))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Raison ou justification invalide")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Signalement introuvable")]
        public async Task<IActionResult> Update(int id, [FromBody] ReportUpdateRequest request) =>
            await _builder.UpdateAsync(id, request) is { } report ? Ok(report) : NotFound();

        [HttpDelete("admin/{id:int}")]
        [Authorize(Policy = Permissions.Reports.Manage)]
        [SwaggerResponse(StatusCodes.Status204NoContent, "Signalement supprimé définitivement")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Signalement introuvable")]
        public async Task<IActionResult> Delete(int id) => await _builder.DeleteAsync(id) ? NoContent() : NotFound();
    }
}
