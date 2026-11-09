using HelpMuslimWebApp.Api.Builders;
using HelpMuslimWebApp.Api.Request;
using HelpMuslimWebApp.Api.Utils;
using HelpMuslimWebApp.Api.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace HelpMuslimWebApp.Api.Controllers
{
    // Coach IA du candidat (Premium) : analyse de son CV ou de son profil, conseils pour mieux se présenter
    [ApiController]
    [Route("candidates/me/coach")]
    [Authorize(Policy = AuthorizationPolicies.Candidat)]
    public class CvCoachController : ControllerBase
    {
        private readonly ICvCoachViewModelBuilder _builder;

        public CvCoachController(ICvCoachViewModelBuilder builder)
        {
            _builder = builder;
        }

        [HttpGet]
        [SwaggerResponse(StatusCodes.Status200OK, "État du coach et dernière analyse", typeof(CvCoachViewModel))]
        public async Task<IActionResult> Get() => Ok(await _builder.GetAsync(User));

        // Lance une analyse (de 20 secondes à une minute environ) ; source = cv (CV et profil) ou profile (profil seul)
        [HttpPost("analyses")]
        [SwaggerResponse(StatusCodes.Status200OK, "Analyse faite", typeof(CvCoachViewModel))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Ni CV lisible ni profil rempli")]
        [SwaggerResponse(StatusCodes.Status409Conflict, "Premium requis, démo, coach indisponible ou quota du mois atteint")]
        public async Task<IActionResult> Analyze([FromQuery] string? source, CancellationToken cancellationToken) => Ok(await _builder.AnalyzeAsync(User, source, cancellationToken));

        // Supprime l'analyse affichée et les précédentes : résultat effacé, le quota du mois n'est pas rendu
        [HttpDelete("analyses")]
        [SwaggerResponse(StatusCodes.Status200OK, "Analyses supprimées", typeof(CvCoachViewModel))]
        [SwaggerResponse(StatusCodes.Status409Conflict, "Démo : lecture seule")]
        public async Task<IActionResult> DeleteAnalyses() => Ok(await _builder.DeleteAnalysesAsync(User));

        // « Ajouter à mes compétences » depuis une suggestion du coach
        [HttpPost("skills")]
        [SwaggerResponse(StatusCodes.Status200OK, "Compétences du profil", typeof(List<string>))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Compétence vide ou trop longue")]
        public async Task<IActionResult> AddSkill([FromBody] CoachSkillRequest model) => Ok(await _builder.AddSkillAsync(User, model.Skill!));
    }
}
