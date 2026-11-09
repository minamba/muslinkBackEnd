using HelpMuslimWebApp.Api.Utils;
using HelpMuslimWebApp.Api.Builders;
using HelpMuslimWebApp.Api.ViewModels;
using HelpMuslimWebApp.Domain.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace HelpMuslimWebApp.Api.Controllers
{
    // Super admin (Administration › Candidats) : coach IA sur le CV ou le profil d'un candidat, sans limite.
    // Ses analyses ne sont jamais montrées au candidat et ne comptent pas dans son quota.
    [ApiController]
    [Route("candidates/{id:int}/coach")]
    [Authorize]
    public class AdminCvCoachController : ControllerBase
    {
        private readonly ICvCoachViewModelBuilder _builder;

        public AdminCvCoachController(ICvCoachViewModelBuilder builder)
        {
            _builder = builder;
        }

        private bool CanUseCoach => User.HasPermission(Permissions.Candidates.Coach);

        [HttpGet]
        [SwaggerResponse(StatusCodes.Status200OK, "État du coach et dernière analyse du super admin pour ce candidat", typeof(CvCoachViewModel))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "Réservé au super admin")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Candidat introuvable")]
        public async Task<IActionResult> Get(int id)
        {
            if (!CanUseCoach) return Forbid();
            return await _builder.GetForAdminAsync(id) is { } coach ? Ok(coach) : NotFound();
        }

        [HttpPost("analyses")]
        [SwaggerResponse(StatusCodes.Status200OK, "Analyse faite", typeof(CvCoachViewModel))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Ni CV lisible ni profil rempli")]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "Réservé au super admin")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Candidat introuvable")]
        [SwaggerResponse(StatusCodes.Status409Conflict, "Démo ou coach non configuré")]
        public async Task<IActionResult> Analyze(int id, [FromQuery] string? source, CancellationToken cancellationToken)
        {
            if (!CanUseCoach) return Forbid();
            return await _builder.AnalyzeForAdminAsync(id, source, cancellationToken) is { } coach ? Ok(coach) : NotFound();
        }
    }
}
