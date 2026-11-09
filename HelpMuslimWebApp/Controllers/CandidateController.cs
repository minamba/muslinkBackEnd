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
    [Route("candidates")]
    [Authorize]
    public class CandidateController : Controller
    {
        private readonly ICandidateProfileViewModelBuilder _builder;

        public CandidateController(ICandidateProfileViewModelBuilder builder)
        {
            _builder = builder ?? throw new ArgumentNullException(nameof(builder));
        }

        // ---------- Espace candidat ----------

        [HttpGet("me")]
        [Authorize(Policy = AuthorizationPolicies.Candidat)]
        [SwaggerResponse(StatusCodes.Status200OK, "Mon profil candidat", typeof(CandidateProfileViewModel))]
        public async Task<IActionResult> GetMyProfile() => Ok(await _builder.GetMyProfileAsync(User));

        [HttpPut("me")]
        [Authorize(Policy = AuthorizationPolicies.Candidat)]
        [SwaggerResponse(StatusCodes.Status200OK, "Profil mis à jour", typeof(CandidateProfileViewModel))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Données invalides")]
        public async Task<IActionResult> UpdateMyProfile([FromBody] CandidateProfileRequest model)
        {
            var result = await _builder.UpdateMyProfileAsync(User, model);
            return result == null ? NotFound() : Ok(result);
        }

        [HttpPost("me/photo")]
        [Authorize(Policy = AuthorizationPolicies.Candidat)]
        [SwaggerResponse(StatusCodes.Status200OK, "Photo enregistrée", typeof(CandidateProfileViewModel))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Format ou taille invalide")]
        public async Task<IActionResult> UploadPhoto(IFormFile file) => Ok(await _builder.UpdateMyPhotoAsync(User, file));

        [HttpDelete("me/photo")]
        [Authorize(Policy = AuthorizationPolicies.Candidat)]
        [SwaggerResponse(StatusCodes.Status200OK, "Photo supprimée", typeof(CandidateProfileViewModel))]
        public async Task<IActionResult> DeletePhoto() => Ok(await _builder.DeleteMyPhotoAsync(User));

        [HttpPost("me/cv")]
        [Authorize(Policy = AuthorizationPolicies.Candidat)]
        [SwaggerResponse(StatusCodes.Status200OK, "CV enregistré", typeof(CandidateProfileViewModel))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Format ou taille invalide")]
        public async Task<IActionResult> UploadCv(IFormFile file) => Ok(await _builder.UpdateMyCvAsync(User, file));

        // CV montré ou masqué aux recruteurs sur la fiche (toujours joignable à une candidature)
        [HttpPut("me/cv/visibility")]
        [Authorize(Policy = AuthorizationPolicies.Candidat)]
        [SwaggerResponse(StatusCodes.Status200OK, "Choix enregistré", typeof(CandidateProfileViewModel))]
        public async Task<IActionResult> SetCvVisibility([FromBody] CvVisibilityRequest model) => Ok(await _builder.SetMyCvHiddenAsync(User, model.Hidden));

        [HttpDelete("me/cv")]
        [Authorize(Policy = AuthorizationPolicies.Candidat)]
        [SwaggerResponse(StatusCodes.Status200OK, "CV supprimé", typeof(CandidateProfileViewModel))]
        public async Task<IActionResult> DeleteCv() => Ok(await _builder.DeleteMyCvAsync(User));

        // Profil invisible : absent des suggestions et inaccessible aux recruteurs sans candidature ni échange
        [HttpPut("me/visibility")]
        [Authorize(Policy = AuthorizationPolicies.Candidat)]
        [SwaggerResponse(StatusCodes.Status200OK, "Visibilité du profil modifiée", typeof(CandidateProfileViewModel))]
        public async Task<IActionResult> SetMyVisibility([FromBody] ProfileVisibilityRequest model) =>
            Ok(await _builder.SetMyVisibilityAsync(User, model.IsHidden!.Value));

        // ---------- Partage de la fiche (QR code) ----------

        [HttpGet("me/share")]
        [Authorize(Policy = AuthorizationPolicies.Candidat)]
        [SwaggerResponse(StatusCodes.Status200OK, "État du partage de ma fiche", typeof(CandidateShareViewModel))]
        public async Task<IActionResult> GetMyShare() => Ok(await _builder.GetMyShareAsync(User));

        [HttpPut("me/share")]
        [Authorize(Policy = AuthorizationPolicies.Candidat)]
        [SwaggerResponse(StatusCodes.Status200OK, "Partage activé (renew = nouveau lien)", typeof(CandidateShareViewModel))]
        public async Task<IActionResult> EnableMyShare([FromBody] CandidateShareRequest model) => Ok(await _builder.EnableMyShareAsync(User, model));

        [HttpDelete("me/share")]
        [Authorize(Policy = AuthorizationPolicies.Candidat)]
        [SwaggerResponse(StatusCodes.Status200OK, "Partage désactivé : le QR code ne fonctionne plus", typeof(CandidateShareViewModel))]
        public async Task<IActionResult> DisableMyShare() => Ok(await _builder.DisableMyShareAsync(User));

        // GET /candidates/shared/{token} → fiche publique ouverte depuis le QR code
        [HttpGet("shared/{token}")]
        [AllowAnonymous]
        [SwaggerResponse(StatusCodes.Status200OK, "Fiche publique du candidat", typeof(SharedCandidateProfileViewModel))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Lien invalide ou partage désactivé")]
        public async Task<IActionResult> GetSharedProfile(string token)
        {
            var result = await _builder.GetSharedProfileAsync(token);
            return result == null ? NotFound() : Ok(result);
        }

        // ---------- Consultation (recruteurs, administration) ----------

        // GET /candidates/matching?search=&offerId=&city=&availableWithinMonths=&take= → candidats classés selon les offres
        // ouvertes du recruteur (ou l'une d'elles), filtrés par ville et disponibilité
        [HttpGet("matching")]
        [Authorize(Policy = AuthorizationPolicies.Recruteur)]
        [SwaggerResponse(StatusCodes.Status200OK, "Candidats du plus proche au plus éloigné", typeof(IEnumerable<CandidateMatchViewModel>))]
        public async Task<IActionResult> GetMatchingCandidates([FromQuery] CandidateMatchQuery query) =>
            Ok(await _builder.GetMatchingCandidatesAsync(User, query));

        // GET /candidates/matching/offers?search=&city=&availableWithinMonths= → candidats en lien avec chaque offre ouverte
        [HttpGet("matching/offers")]
        [Authorize(Policy = AuthorizationPolicies.Recruteur)]
        [SwaggerResponse(StatusCodes.Status200OK, "Nombre de candidats par offre", typeof(IEnumerable<OfferMatchCountViewModel>))]
        public async Task<IActionResult> GetMatchingOfferCounts([FromQuery] CandidateMatchQuery query) =>
            Ok(await _builder.GetMatchingOfferCountsAsync(User, query));

        // GET /candidates/matching/cities → villes des candidats et leur nombre (filtre « Ville »)
        [HttpGet("matching/cities")]
        [Authorize(Policy = AuthorizationPolicies.Recruteur)]
        [SwaggerResponse(StatusCodes.Status200OK, "Villes des candidats", typeof(IEnumerable<CityCountViewModel>))]
        public async Task<IActionResult> GetMatchingCities() => Ok(await _builder.GetMatchingCitiesAsync(User));

        [HttpGet("{id:int}")]
        [SwaggerResponse(StatusCodes.Status200OK, "Profil complet du candidat", typeof(CandidateProfileViewModel))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "Accès refusé")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Candidat introuvable")]
        public async Task<IActionResult> GetCandidate(int id)
        {
            var result = await _builder.GetCandidateProfileByIdAsync(id, User);
            return result == null ? NotFound() : Ok(result);
        }

        [HttpGet("{id:int}/cv")]
        [SwaggerResponse(StatusCodes.Status200OK, "Fichier CV")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Pas de CV")]
        public async Task<IActionResult> DownloadCv(int id)
        {
            var cv = await _builder.GetCvAsync(id, User);
            return cv == null ? NotFound() : File(cv.Value.Stream, cv.Value.ContentType, cv.Value.FileName);
        }

        // ---------- Administration ----------

        [HttpGet]
        [Authorize(Policy = Permissions.Candidates.View)]
        [SwaggerResponse(StatusCodes.Status200OK, "Liste des candidats", typeof(IEnumerable<CandidateSummaryViewModel>))]
        public async Task<IActionResult> GetCandidates([FromQuery] string? search) => Ok(await _builder.GetCandidateProfilesAsync(search));

        [HttpPost]
        [Authorize(Policy = Permissions.Candidates.Create)]
        [SwaggerResponse(StatusCodes.Status200OK, "Profil créé", typeof(CandidateProfileViewModel))]
        [SwaggerResponse(StatusCodes.Status409Conflict, "Profil déjà existant")]
        public async Task<IActionResult> AddCandidate([FromBody] ProfileCreateRequest model)
        {
            var result = await _builder.AddCandidateProfileAsync(model);
            return result == null ? Conflict(new { message = "Ce compte a déjà un profil candidat." }) : Ok(result);
        }

        [HttpPut("{id:int}")]
        [Authorize(Policy = Permissions.Candidates.Edit)]
        [SwaggerResponse(StatusCodes.Status200OK, "Profil modifié", typeof(CandidateProfileViewModel))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Candidat introuvable")]
        public async Task<IActionResult> UpdateCandidate(int id, [FromBody] AdminCandidateProfileRequest model)
        {
            var result = await _builder.UpdateCandidateProfileAsync(id, model);
            return result == null ? NotFound() : Ok(result);
        }

        [HttpDelete("{id:int}")]
        [Authorize(Policy = Permissions.Candidates.Delete)]
        [SwaggerResponse(StatusCodes.Status200OK, "Profil supprimé")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Candidat introuvable")]
        public async Task<IActionResult> DeleteCandidate(int id) => await _builder.DeleteCandidateProfileAsync(id) ? Ok() : NotFound();
    }
}
