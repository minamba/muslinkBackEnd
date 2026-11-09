using HelpMuslimWebApp.Api.Builders;
using HelpMuslimWebApp.Api.Utils;
using HelpMuslimWebApp.Api.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace HelpMuslimWebApp.Api.Controllers
{
    [ApiController]
    [Route("favorites")]
    [Authorize(Policy = AuthorizationPolicies.Recruteur)]
    public class FavoriteController : Controller
    {
        private readonly IFavoriteViewModelBuilder _builder;

        public FavoriteController(IFavoriteViewModelBuilder builder)
        {
            _builder = builder ?? throw new ArgumentNullException(nameof(builder));
        }

        [HttpGet]
        [SwaggerResponse(StatusCodes.Status200OK, "Mes candidats favoris", typeof(IEnumerable<FavoriteViewModel>))]
        public async Task<IActionResult> GetFavorites() => Ok(await _builder.GetMyFavoritesAsync(User));

        [HttpPost("{candidateId:int}")]
        [SwaggerResponse(StatusCodes.Status200OK, "Candidat ajouté aux favoris")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Candidat introuvable")]
        public async Task<IActionResult> AddFavorite(int candidateId) =>
            await _builder.AddFavoriteAsync(User, candidateId) ? Ok() : NotFound();

        [HttpDelete("{candidateId:int}")]
        [SwaggerResponse(StatusCodes.Status200OK, "Candidat retiré des favoris")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Pas dans les favoris")]
        public async Task<IActionResult> RemoveFavorite(int candidateId) =>
            await _builder.RemoveFavoriteAsync(User, candidateId) ? Ok() : NotFound();
    }
}
