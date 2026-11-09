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
    [Route("offers")]
    [Authorize]
    public class JobOfferController : Controller
    {
        private readonly IJobOfferViewModelBuilder _builder;

        public JobOfferController(IJobOfferViewModelBuilder builder)
        {
            _builder = builder ?? throw new ArgumentNullException(nameof(builder));
        }

        // GET /offers?domainId=&professionId=&contractType=&search=&city=&page=&pageSize=
        [HttpGet]
        [AllowAnonymous]
        [SwaggerResponse(StatusCodes.Status200OK, "Offres ouvertes (paginées), triées par correspondance pour un candidat connecté", typeof(PagedViewModel<JobOfferViewModel>))]
        public async Task<IActionResult> GetJobOffers([FromQuery] OfferQueryRequest query) => Ok(await _builder.GetJobOffersAsync(query, User));

        [HttpGet("all")]
        [Authorize(Policy = Permissions.Offers.View)]
        [SwaggerResponse(StatusCodes.Status200OK, "Toutes les offres, y compris fermées", typeof(PagedViewModel<JobOfferViewModel>))]
        public async Task<IActionResult> GetAllJobOffers([FromQuery] OfferQueryRequest query) => Ok(await _builder.GetAllJobOffersAsync(query));

        [HttpGet("mine")]
        [Authorize(Policy = AuthorizationPolicies.Recruteur)]
        [SwaggerResponse(StatusCodes.Status200OK, "Mes offres (recruteur)", typeof(IEnumerable<JobOfferViewModel>))]
        public async Task<IActionResult> GetMyJobOffers() => Ok(await _builder.GetMyJobOffersAsync(User));

        // GET /offers/filled?take= → nombre d'offres pourvues + dernières en date (bandeau "en direct" de l'accueil)
        [HttpGet("filled")]
        [AllowAnonymous]
        [SwaggerResponse(StatusCodes.Status200OK, "Offres pourvues grâce à Muslink", typeof(FilledOffersViewModel))]
        public async Task<IActionResult> GetFilledOffers([FromQuery] int take = 10) => Ok(await _builder.GetFilledOffersAsync(take));

        // DELETE /offers/filled?recruiterProfileId= → retire du compteur les offres pourvues d'un recruteur (administration)
        [HttpDelete("filled")]
        [Authorize(Policy = Permissions.Offers.Delete)]
        [SwaggerResponse(StatusCodes.Status200OK, "Nombre d'entrées retirées du compteur", typeof(int))]
        public async Task<IActionResult> DeleteOfferFills([FromQuery] int recruiterProfileId) => Ok(await _builder.DeleteOfferFillsAsync(recruiterProfileId));

        // GET /offers/wishes?take= → offres qui correspondent aux vœux du candidat (« Ce que je cherche ») à 70 % au moins
        [HttpGet("wishes")]
        [Authorize(Policy = AuthorizationPolicies.Candidat)]
        [SwaggerResponse(StatusCodes.Status200OK, "Offres selon mes vœux, de la plus proche à la plus éloignée", typeof(WishOffersViewModel))]
        public async Task<IActionResult> GetWishOffers([FromQuery] int? take) => Ok(await _builder.GetWishOffersAsync(User, take));

        // ---------- Offres favorites du candidat ----------

        [HttpGet("favorites")]
        [Authorize(Policy = AuthorizationPolicies.Candidat)]
        [SwaggerResponse(StatusCodes.Status200OK, "Mes offres favorites (les plus récentes en premier)", typeof(IEnumerable<JobOfferViewModel>))]
        public async Task<IActionResult> GetFavoriteOffers() => Ok(await _builder.GetFavoriteOffersAsync(User));

        [HttpPut("{id:int}/favorite")]
        [Authorize(Policy = AuthorizationPolicies.Candidat)]
        [SwaggerResponse(StatusCodes.Status200OK, "Offre ajoutée aux favoris")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Offre introuvable")]
        [SwaggerResponse(StatusCodes.Status409Conflict, "Offre fermée")]
        public async Task<IActionResult> AddFavoriteOffer(int id) => await _builder.AddFavoriteOfferAsync(User, id) ? Ok() : NotFound();

        [HttpDelete("{id:int}/favorite")]
        [Authorize(Policy = AuthorizationPolicies.Candidat)]
        [SwaggerResponse(StatusCodes.Status200OK, "Offre retirée des favoris")]
        public async Task<IActionResult> RemoveFavoriteOffer(int id)
        {
            await _builder.RemoveFavoriteOfferAsync(User, id);
            return Ok();
        }

        [HttpGet("{id:int}")]
        [AllowAnonymous]
        [SwaggerResponse(StatusCodes.Status200OK, "Détail de l'offre", typeof(JobOfferViewModel))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Offre introuvable ou fermée")]
        public async Task<IActionResult> GetJobOffer(int id)
        {
            var result = await _builder.GetJobOfferByIdAsync(id, User);
            return result == null ? NotFound() : Ok(result);
        }

        [HttpPost]
        [SwaggerResponse(StatusCodes.Status200OK, "Offre publiée", typeof(JobOfferViewModel))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Données invalides")]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "Réservé aux recruteurs")]
        public async Task<IActionResult> AddJobOffer([FromBody] JobOfferRequest model) => Ok(await _builder.AddJobOfferAsync(User, model));

        [HttpPut]
        [SwaggerResponse(StatusCodes.Status200OK, "Offre modifiée", typeof(JobOfferViewModel))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "Pas propriétaire de l'offre")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Offre introuvable")]
        public async Task<IActionResult> UpdateJobOffer([FromBody] JobOfferRequest model)
        {
            var result = await _builder.UpdateJobOfferAsync(User, model);
            return result == null ? NotFound() : Ok(result);
        }

        // Ouvrir / fermer une offre (une offre fermée n'est plus visible des candidats) ; isFilled : candidat trouvé
        [HttpPatch("{id:int}/status")]
        [SwaggerResponse(StatusCodes.Status200OK, "Statut modifié", typeof(JobOfferViewModel))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "Pas propriétaire de l'offre")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Offre introuvable")]
        public async Task<IActionResult> SetJobOfferStatus(int id, [FromBody] OfferStatusRequest model)
        {
            var result = await _builder.SetJobOfferStatusAsync(User, id, model.IsOpen!.Value, model.IsFilled == true, model.HiredCandidateProfileId);
            return result == null ? NotFound() : Ok(result);
        }

        // Masquer / rendre visible une offre (icône œil) : une offre masquée n'apparaît plus publiquement
        [HttpPatch("{id:int}/visibility")]
        [SwaggerResponse(StatusCodes.Status200OK, "Visibilité modifiée", typeof(JobOfferViewModel))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "Pas propriétaire de l'offre")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Offre introuvable")]
        public async Task<IActionResult> SetJobOfferVisibility(int id, [FromBody] OfferVisibilityRequest model)
        {
            var result = await _builder.SetJobOfferVisibilityAsync(User, id, model.IsHidden!.Value);
            return result == null ? NotFound() : Ok(result);
        }

        [HttpDelete("{id:int}")]
        [SwaggerResponse(StatusCodes.Status200OK, "Offre supprimée")]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "Pas propriétaire de l'offre")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Offre introuvable")]
        public async Task<IActionResult> DeleteJobOffer(int id) => await _builder.DeleteJobOfferAsync(User, id) ? Ok() : NotFound();
    }
}
