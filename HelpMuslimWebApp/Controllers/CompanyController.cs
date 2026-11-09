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
    // Annuaire des entreprises (public) et entreprises suivies par les candidats
    [ApiController]
    [Route("companies")]
    [Authorize]
    public class CompanyController : Controller
    {
        private readonly ICompanyViewModelBuilder _builder;

        public CompanyController(ICompanyViewModelBuilder builder)
        {
            _builder = builder ?? throw new ArgumentNullException(nameof(builder));
        }

        // GET /companies?search=&sector=&take= → entreprises, celles qui recrutent le plus en premier
        [HttpGet]
        [AllowAnonymous]
        [SwaggerResponse(StatusCodes.Status200OK, "Entreprises", typeof(IEnumerable<CompanyViewModel>))]
        public async Task<IActionResult> GetCompanies([FromQuery] string? search, [FromQuery] string? sector, [FromQuery] int? take) =>
            Ok(await _builder.GetCompaniesAsync(search, sector, take, User));

        // GET /companies/sectors → activités présentes dans l'annuaire (filtre)
        [HttpGet("sectors")]
        [AllowAnonymous]
        [SwaggerResponse(StatusCodes.Status200OK, "Activités et nombre d'entreprises", typeof(IEnumerable<CompanySectorViewModel>))]
        public async Task<IActionResult> GetSectors() => Ok(await _builder.GetCompanySectorsAsync());

        [HttpGet("followed")]
        [Authorize(Policy = AuthorizationPolicies.Candidat)]
        [SwaggerResponse(StatusCodes.Status200OK, "Entreprises que je suis", typeof(IEnumerable<CompanyViewModel>))]
        public async Task<IActionResult> GetFollowedCompanies() => Ok(await _builder.GetFollowedCompaniesAsync(User));

        [HttpGet("{id:int}")]
        [AllowAnonymous]
        [SwaggerResponse(StatusCodes.Status200OK, "Entreprise et ses offres ouvertes", typeof(CompanyDetailViewModel))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Entreprise introuvable")]
        public async Task<IActionResult> GetCompany(int id)
        {
            var result = await _builder.GetCompanyAsync(id, User);
            return result == null ? NotFound() : Ok(result);
        }

        // ---------- Page entreprise du recruteur ----------

        [HttpGet("me/page")]
        [Authorize(Policy = AuthorizationPolicies.Recruteur)]
        [SwaggerResponse(StatusCodes.Status200OK, "Ma page entreprise", typeof(CompanyPageViewModel))]
        public async Task<IActionResult> GetMyPage() => Ok(await _builder.GetMyCompanyPageAsync(User));

        [HttpPut("me/page")]
        [Authorize(Policy = AuthorizationPolicies.Recruteur)]
        [SwaggerResponse(StatusCodes.Status200OK, "Page enregistrée", typeof(CompanyPageViewModel))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Liste trop longue ou lien invalide")]
        public async Task<IActionResult> UpdateMyPage([FromBody] CompanyPageRequest model) => Ok(await _builder.UpdateMyCompanyPageAsync(User, model));

        [HttpPost("me/cover")]
        [Authorize(Policy = AuthorizationPolicies.Recruteur)]
        [RequestSizeLimit(6 * 1024 * 1024)]
        [SwaggerResponse(StatusCodes.Status200OK, "Couverture enregistrée", typeof(CompanyPageViewModel))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Format ou taille invalide")]
        public async Task<IActionResult> UploadCover(IFormFile file) => Ok(await _builder.SetMyCoverAsync(User, file));

        [HttpDelete("me/cover")]
        [Authorize(Policy = AuthorizationPolicies.Recruteur)]
        [SwaggerResponse(StatusCodes.Status200OK, "Couverture retirée", typeof(CompanyPageViewModel))]
        public async Task<IActionResult> DeleteCover() => Ok(await _builder.SetMyCoverAsync(User, null));

        [HttpPost("me/photos")]
        [Authorize(Policy = AuthorizationPolicies.Recruteur)]
        [RequestSizeLimit(6 * 1024 * 1024)]
        [SwaggerResponse(StatusCodes.Status200OK, "Photo ajoutée", typeof(CompanyPageViewModel))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Format ou taille invalide")]
        [SwaggerResponse(StatusCodes.Status409Conflict, "Nombre maximal de photos atteint")]
        public async Task<IActionResult> AddPhoto(IFormFile file, [FromForm] string? caption) => Ok(await _builder.AddMyPhotoAsync(User, file, caption));

        [HttpPost("me/videos")]
        [Authorize(Policy = AuthorizationPolicies.Recruteur)]
        [SwaggerResponse(StatusCodes.Status200OK, "Vidéo ajoutée", typeof(CompanyPageViewModel))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Lien YouTube ou Vimeo invalide, ou vidéo Vimeo privée")]
        [SwaggerResponse(StatusCodes.Status409Conflict, "Nombre maximal de vidéos atteint")]
        public async Task<IActionResult> AddVideo([FromBody] CompanyVideoRequest model) => Ok(await _builder.AddMyVideoAsync(User, model));

        [HttpDelete("me/media/{id:int}")]
        [Authorize(Policy = AuthorizationPolicies.Recruteur)]
        [SwaggerResponse(StatusCodes.Status200OK, "Photo ou vidéo retirée", typeof(CompanyPageViewModel))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Introuvable sur votre page")]
        public async Task<IActionResult> DeleteMedia(int id)
        {
            var result = await _builder.DeleteMyMediaAsync(User, id);
            return result == null ? NotFound() : Ok(result);
        }

        // ---------- Administration ----------

        [HttpGet("admin")]
        [Authorize(Policy = Permissions.Companies.View)]
        [SwaggerResponse(StatusCodes.Status200OK, "Toutes les entreprises, désactivées comprises", typeof(IEnumerable<CompanyAdminViewModel>))]
        public async Task<IActionResult> GetAdminCompanies([FromQuery] string? search) => Ok(await _builder.GetAdminCompaniesAsync(search));

        // Désactiver : l'entreprise disparaît de l'annuaire et toutes ses offres sont cachées ; réactiver les rend à nouveau visibles
        [HttpPatch("{id:int}/status")]
        [Authorize(Policy = Permissions.Companies.Disable)]
        [SwaggerResponse(StatusCodes.Status200OK, "Entreprise désactivée ou réactivée", typeof(CompanyAdminViewModel))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Entreprise introuvable")]
        public async Task<IActionResult> SetStatus(int id, [FromBody] CompanyStatusRequest model)
        {
            var result = await _builder.SetCompanyDisabledAsync(id, model.IsDisabled!.Value);
            return result == null ? NotFound() : Ok(result);
        }

        // ---------- Candidats ----------

        [HttpPut("{id:int}/follow")]
        [Authorize(Policy = AuthorizationPolicies.Candidat)]
        [SwaggerResponse(StatusCodes.Status200OK, "Entreprise suivie : alerte et email à chaque nouvelle offre")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Entreprise introuvable")]
        public async Task<IActionResult> Follow(int id) => await _builder.FollowCompanyAsync(User, id) ? Ok() : NotFound();

        [HttpDelete("{id:int}/follow")]
        [Authorize(Policy = AuthorizationPolicies.Candidat)]
        [SwaggerResponse(StatusCodes.Status200OK, "Entreprise plus suivie")]
        public async Task<IActionResult> Unfollow(int id)
        {
            await _builder.UnfollowCompanyAsync(User, id);
            return Ok();
        }
    }
}
