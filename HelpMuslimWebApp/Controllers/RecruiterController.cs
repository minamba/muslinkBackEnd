using HelpMuslimWebApp.Api.Builders;
using HelpMuslimWebApp.Api.Request;
using HelpMuslimWebApp.Api.Utils;
using HelpMuslimWebApp.Api.ViewModels;
using HelpMuslimWebApp.Domain.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace HelpMuslimWebApp.Api.Controllers
{
    [ApiController]
    [Route("recruiters")]
    [Authorize]
    public class RecruiterController : Controller
    {
        private readonly IRecruiterProfileViewModelBuilder _builder;

        public RecruiterController(IRecruiterProfileViewModelBuilder builder)
        {
            _builder = builder ?? throw new ArgumentNullException(nameof(builder));
        }

        // ---------- Espace recruteur ----------

        [HttpGet("me")]
        [Authorize(Policy = AuthorizationPolicies.Recruteur)]
        [SwaggerResponse(StatusCodes.Status200OK, "Mon profil recruteur", typeof(RecruiterProfileViewModel))]
        public async Task<IActionResult> GetMyProfile() => Ok(await _builder.GetMyProfileAsync(User));

        [HttpPut("me")]
        [Authorize(Policy = AuthorizationPolicies.Recruteur)]
        [SwaggerResponse(StatusCodes.Status200OK, "Profil mis à jour", typeof(RecruiterProfileViewModel))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Données invalides")]
        public async Task<IActionResult> UpdateMyProfile([FromBody] RecruiterProfileRequest model)
        {
            var result = await _builder.UpdateMyProfileAsync(User, model);
            return result == null ? NotFound() : Ok(result);
        }

        [HttpPut("me/match-alerts")]
        [Authorize(Policy = AuthorizationPolicies.Recruteur)]
        [SwaggerResponse(StatusCodes.Status200OK, "Alerte des nouveaux candidats activée ou coupée", typeof(RecruiterProfileViewModel))]
        public async Task<IActionResult> SetMyMatchAlerts([FromBody] MatchAlertsRequest model) =>
            Ok(await _builder.SetMyMatchAlertsAsync(User, model.Enabled!.Value));

        // Lien « Ne plus recevoir ces alertes » de l'email : sans connexion, le jeton identifie le recruteur
        [HttpPost("match-alerts")]
        [AllowAnonymous]
        [SwaggerResponse(StatusCodes.Status200OK, "Alerte des nouveaux candidats activée ou coupée")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Lien invalide")]
        public async Task<IActionResult> SetMatchAlertsByToken([FromBody] MatchAlertsTokenRequest model) =>
            await _builder.SetMatchAlertsByTokenAsync(model.Token!, model.Enabled!.Value) ? Ok() : NotFound();

        [HttpPut("me/message-alerts")]
        [Authorize(Policy = AuthorizationPolicies.Recruteur)]
        [SwaggerResponse(StatusCodes.Status200OK, "Email « un candidat vous a écrit » activé ou coupé", typeof(RecruiterProfileViewModel))]
        public async Task<IActionResult> SetMyMessageAlerts([FromBody] MatchAlertsRequest model) =>
            Ok(await _builder.SetMyMessageAlertsAsync(User, model.Enabled!.Value));

        // Lien « Ne plus recevoir ces emails » de l'email de message : sans connexion, le jeton identifie le recruteur
        [HttpPost("message-alerts")]
        [AllowAnonymous]
        [SwaggerResponse(StatusCodes.Status200OK, "Email « un candidat vous a écrit » activé ou coupé")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Lien invalide")]
        public async Task<IActionResult> SetMessageAlertsByToken([FromBody] MatchAlertsTokenRequest model) =>
            await _builder.SetMessageAlertsByTokenAsync(model.Token!, model.Enabled!.Value) ? Ok() : NotFound();

        [HttpPut("me/application-alerts")]
        [Authorize(Policy = AuthorizationPolicies.Recruteur)]
        [SwaggerResponse(StatusCodes.Status200OK, "Email « nouvelle candidature » activé ou coupé", typeof(RecruiterProfileViewModel))]
        public async Task<IActionResult> SetMyApplicationAlerts([FromBody] MatchAlertsRequest model) =>
            Ok(await _builder.SetMyApplicationAlertsAsync(User, model.Enabled!.Value));

        // Lien « Ne plus recevoir ces emails » de l'email de candidature : sans connexion, le jeton identifie le recruteur
        [HttpPost("application-alerts")]
        [AllowAnonymous]
        [SwaggerResponse(StatusCodes.Status200OK, "Email « nouvelle candidature » activé ou coupé")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Lien invalide")]
        public async Task<IActionResult> SetApplicationAlertsByToken([FromBody] MatchAlertsTokenRequest model) =>
            await _builder.SetApplicationAlertsByTokenAsync(model.Token!, model.Enabled!.Value) ? Ok() : NotFound();

        [HttpPost("me/logo")]
        [Authorize(Policy = AuthorizationPolicies.Recruteur)]
        [SwaggerResponse(StatusCodes.Status200OK, "Logo enregistré", typeof(RecruiterProfileViewModel))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Format ou taille invalide")]
        public async Task<IActionResult> UploadLogo(IFormFile file) => Ok(await _builder.UpdateMyLogoAsync(User, file));

        [HttpDelete("me/logo")]
        [Authorize(Policy = AuthorizationPolicies.Recruteur)]
        [SwaggerResponse(StatusCodes.Status200OK, "Logo supprimé", typeof(RecruiterProfileViewModel))]
        public async Task<IActionResult> DeleteLogo() => Ok(await _builder.DeleteMyLogoAsync(User));

        // ---------- Fiche entreprise (publique) ----------

        [HttpGet("{id:int}")]
        [AllowAnonymous]
        [SwaggerResponse(StatusCodes.Status200OK, "Fiche entreprise", typeof(RecruiterProfileViewModel))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Recruteur introuvable")]
        public async Task<IActionResult> GetRecruiter(int id)
        {
            var result = await _builder.GetRecruiterProfileByIdAsync(id, User);
            return result == null ? NotFound() : Ok(result);
        }

        // ---------- Administration ----------

        [HttpGet]
        [Authorize(Policy = Permissions.Recruiters.View)]
        [SwaggerResponse(StatusCodes.Status200OK, "Liste des recruteurs", typeof(IEnumerable<RecruiterProfileViewModel>))]
        public async Task<IActionResult> GetRecruiters([FromQuery] string? search) => Ok(await _builder.GetRecruiterProfilesAsync(search));

        [HttpPost]
        [Authorize(Policy = Permissions.Recruiters.Create)]
        [SwaggerResponse(StatusCodes.Status200OK, "Profil créé", typeof(RecruiterProfileViewModel))]
        [SwaggerResponse(StatusCodes.Status409Conflict, "Profil déjà existant")]
        public async Task<IActionResult> AddRecruiter([FromBody] ProfileCreateRequest model)
        {
            var result = await _builder.AddRecruiterProfileAsync(model);
            return result == null ? Conflict(new { message = "Ce compte a déjà un profil recruteur." }) : Ok(result);
        }

        [HttpPut("{id:int}")]
        [Authorize(Policy = Permissions.Recruiters.Edit)]
        [SwaggerResponse(StatusCodes.Status200OK, "Profil modifié", typeof(RecruiterProfileViewModel))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Recruteur introuvable")]
        public async Task<IActionResult> UpdateRecruiter(int id, [FromBody] AdminRecruiterProfileRequest model)
        {
            var result = await _builder.UpdateRecruiterProfileAsync(id, model);
            return result == null ? NotFound() : Ok(result);
        }

        [HttpDelete("{id:int}")]
        [Authorize(Policy = Permissions.Recruiters.Delete)]
        [SwaggerResponse(StatusCodes.Status200OK, "Profil supprimé (avec ses offres)")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Recruteur introuvable")]
        public async Task<IActionResult> DeleteRecruiter(int id) => await _builder.DeleteRecruiterProfileAsync(id) ? Ok() : NotFound();
    }
}
