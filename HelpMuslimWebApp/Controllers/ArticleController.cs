using HelpMuslimWebApp.Api.Builders;
using HelpMuslimWebApp.Api.Request;
using HelpMuslimWebApp.Api.ViewModels;
using HelpMuslimWebApp.Domain.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace HelpMuslimWebApp.Api.Controllers
{
    [ApiController]
    [Route("articles")]
    public class ArticleController : Controller
    {
        private const long MaxUploadBytes = 110 * 1024 * 1024;   // vidéos jusqu'à 100 Mo

        private readonly IArticleViewModelBuilder _builder;

        public ArticleController(IArticleViewModelBuilder builder)
        {
            _builder = builder ?? throw new ArgumentNullException(nameof(builder));
        }

        // GET /articles?category=&search=&page=&pageSize=
        [HttpGet]
        [SwaggerResponse(StatusCodes.Status200OK, "Actualités (paginées, sans le contenu)", typeof(PagedViewModel<ArticleViewModel>))]
        public async Task<IActionResult> GetArticles([FromQuery] ArticleQueryRequest query) => Ok(await _builder.GetArticlesAsync(query));

        [HttpGet("categories")]
        [SwaggerResponse(StatusCodes.Status200OK, "Catégories utilisées", typeof(IEnumerable<string>))]
        public async Task<IActionResult> GetCategories() => Ok(await _builder.GetCategoriesAsync());

        [HttpGet("{id:int}")]
        [SwaggerResponse(StatusCodes.Status200OK, "Article complet", typeof(ArticleViewModel))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Article introuvable")]
        public async Task<IActionResult> GetArticle(int id)
        {
            var result = await _builder.GetArticleByIdAsync(id);
            return result == null ? NotFound() : Ok(result);
        }

        [HttpPost]
        [Authorize(Policy = Permissions.Articles.Create)]
        [SwaggerResponse(StatusCodes.Status200OK, "Article publié", typeof(ArticleViewModel))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Données invalides")]
        public async Task<IActionResult> AddArticle([FromBody] ArticleRequest model) => Ok(await _builder.AddArticleAsync(User, model));

        [HttpPut]
        [Authorize(Policy = Permissions.Articles.Edit)]
        [SwaggerResponse(StatusCodes.Status200OK, "Article modifié", typeof(ArticleViewModel))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Article introuvable")]
        public async Task<IActionResult> UpdateArticle([FromBody] ArticleRequest model)
        {
            var result = await _builder.UpdateArticleAsync(User, model);
            return result == null ? NotFound() : Ok(result);
        }

        [HttpDelete("{id:int}")]
        [Authorize(Policy = Permissions.Articles.Delete)]
        [SwaggerResponse(StatusCodes.Status200OK, "Article supprimé")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Article introuvable")]
        public async Task<IActionResult> DeleteArticle(int id) => await _builder.DeleteArticleAsync(id) ? Ok() : NotFound();

        // Image, vidéo ou document inséré dans un article (ou image de couverture)
        [HttpPost("media")]
        [Authorize(Policy = Permissions.Articles.Create)]
        [RequestSizeLimit(MaxUploadBytes)]
        [RequestFormLimits(MultipartBodyLengthLimit = MaxUploadBytes)]
        [SwaggerResponse(StatusCodes.Status200OK, "Média enregistré", typeof(ArticleMediaViewModel))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Format ou taille invalide")]
        public async Task<IActionResult> UploadMedia(IFormFile file) => Ok(await _builder.UploadMediaAsync(file));
    }
}
