using System.Net;
using System.Security.Claims;
using System.Text.RegularExpressions;
using AutoMapper;
using HelpMuslimWebApp.Api.Request;
using HelpMuslimWebApp.Api.Services;
using HelpMuslimWebApp.Api.Utils;
using HelpMuslimWebApp.Api.ViewModels;
using HelpMuslimWebApp.Domain.Models;
using HelpMuslimWebApp.Domain.Services;

namespace HelpMuslimWebApp.Api.Builders.impl
{
    public class ArticleViewModelBuilder : IArticleViewModelBuilder
    {
        private const string CoverPrefix = "/uploads/articles/images/";
        // Balises de bloc → espace ; balises en ligne (strong, em, a…) → rien, pour garder "gras, italique"
        private static readonly Regex BlockTags = new(@"</?(p|h\d|li|ul|ol|blockquote|div|br|hr|figure|figcaption|pre|iframe|video|img)\b[^>]*>", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex Tags = new("<[^>]+>", RegexOptions.Compiled);
        private static readonly Regex Spaces = new(@"\s+", RegexOptions.Compiled);
        private static readonly Regex FirstImage = new("<img[^>]+src=\"([^\"]+)\"", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly IMapper _mapper;
        private readonly IArticleService _articleService;
        private readonly IHtmlContentSanitizer _sanitizer;
        private readonly IFileStorageService _fileStorage;
        private readonly ILogger<ArticleViewModelBuilder> _logger;

        public ArticleViewModelBuilder(IMapper mapper, IArticleService articleService, IHtmlContentSanitizer sanitizer, IFileStorageService fileStorage, ILogger<ArticleViewModelBuilder> logger)
        {
            _mapper = mapper;
            _articleService = articleService;
            _sanitizer = sanitizer;
            _fileStorage = fileStorage;
            _logger = logger;
        }

        public async Task<PagedViewModel<ArticleViewModel>> GetArticlesAsync(ArticleQueryRequest query)
        {
            var result = await _articleService.GetArticlesAsync(new ArticleFilter
            {
                Category = query.Category,
                Search = query.Search,
                ExcludeId = query.ExcludeId,
                OldestFirst = query.Sort == "ancien",
                Page = query.Page,
                PageSize = query.PageSize,
            });
            return _mapper.Map<PagedViewModel<ArticleViewModel>>(result);
        }

        public Task<IEnumerable<string>> GetCategoriesAsync() => _articleService.GetCategoriesAsync();

        public async Task<ArticleViewModel?> GetArticleByIdAsync(int id)
        {
            var article = await _articleService.GetArticleByIdAsync(id);
            return article == null ? null : _mapper.Map<ArticleViewModel>(article);
        }

        public async Task<ArticleViewModel> AddArticleAsync(ClaimsPrincipal user, ArticleRequest model)
        {
            var article = Prepare(new Article(), model);
            article.AuthorUserId = user.GetUserId();
            article.AuthorName = user.FindFirst("name")?.Value ?? user.GetEmail();
            return _mapper.Map<ArticleViewModel>(await _articleService.AddArticleAsync(article));
        }

        public async Task<ArticleViewModel?> UpdateArticleAsync(ClaimsPrincipal user, ArticleRequest model)
        {
            var existing = await _articleService.GetArticleByIdAsync(model.Id);
            if (existing == null) return null;
            var before = ArticleMedia.Paths(existing.Content, existing.CoverImagePath, existing.ThumbnailPath);

            var article = Prepare(existing, model);
            article.UpdatedByName = user.FindFirst("name")?.Value ?? user.GetEmail();
            var saved = await _articleService.UpdateArticleAsync(article);
            if (saved == null) return null;

            // Images, vidéos et documents retirés de l'article (ou couverture remplacée) : effacés du disque
            before.ExceptWith(ArticleMedia.Paths(saved.Content, saved.CoverImagePath, saved.ThumbnailPath));
            await DeleteUnusedFilesAsync(before);
            return _mapper.Map<ArticleViewModel>(saved);
        }

        public async Task<bool> DeleteArticleAsync(int id)
        {
            var article = await _articleService.GetArticleByIdAsync(id);
            if (article == null || !await _articleService.DeleteArticleAsync(id)) return false;

            // Fichiers de l'article (couverture, images, vidéos, documents) supprimés avec lui
            await DeleteUnusedFilesAsync(ArticleMedia.Paths(article.Content, article.CoverImagePath, article.ThumbnailPath));
            return true;
        }

        // Efface les fichiers qu'aucun article n'utilise plus (un lien recopié dans un autre article garde son fichier).
        // Une erreur n'empêche pas l'enregistrement : le nettoyage quotidien rattrapera le fichier.
        private async Task DeleteUnusedFilesAsync(HashSet<string> paths)
        {
            if (paths.Count == 0) return;
            try
            {
                var stillUsed = ArticleMedia.Paths([.. await _articleService.GetMediaSourcesAsync()]);
                foreach (var path in paths.Where(p => !stillUsed.Contains(p)))
                    _fileStorage.Delete(path, ArticleMedia.CategoryOf(path));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Fichiers d'article non effacés : {Paths}", string.Join(", ", paths));
            }
        }

        public async Task<ArticleMediaViewModel> UploadMediaAsync(IFormFile file)
        {
            var category = FileStorageService.ArticleCategoryOf(file.FileName)
                ?? throw new ArgumentException("Format non pris en charge. Images : jpg, png, webp, gif — vidéos : mp4, webm — documents : pdf, Word, Excel, PowerPoint, txt.");

            var url = await _fileStorage.SaveAsync(file, category);
            return new ArticleMediaViewModel
            {
                Url = url,
                FileName = Path.GetFileName(file.FileName),
                Kind = category switch
                {
                    FileCategory.ArticleImage => "image",
                    FileCategory.ArticleVideo => "video",
                    _ => "document",
                },
            };
        }

        // Contenu assaini + données dérivées : chapeau, vignette, temps de lecture
        private Article Prepare(Article article, ArticleRequest model)
        {
            var content = _sanitizer.Sanitize(model.Content!);
            var text = Spaces.Replace(WebUtility.HtmlDecode(Tags.Replace(BlockTags.Replace(content, " "), string.Empty)), " ").Trim();
            if (text.Length < 20 && !FirstImage.IsMatch(content))
                throw new ArgumentException("Le contenu de l'article est trop court.");

            var cover = model.CoverImageUrl?.StartsWith(CoverPrefix, StringComparison.Ordinal) == true ? model.CoverImageUrl : null;
            article.Title = model.Title!.Trim();
            article.Summary = string.IsNullOrWhiteSpace(model.Summary) ? null : model.Summary.Trim();
            article.Excerpt = article.Summary ?? (text.Length > 220 ? text[..220].TrimEnd() + "…" : text);
            article.Content = content;
            article.Category = string.IsNullOrWhiteSpace(model.Category) ? null : model.Category.Trim();
            article.CoverImagePath = cover;
            var firstImage = FirstImage.Match(content);
            article.ThumbnailPath = cover ?? (firstImage.Success ? WebUtility.HtmlDecode(firstImage.Groups[1].Value) : null);
            article.ReadingMinutes = Math.Max(1, (int)Math.Round(text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length / 200.0));
            return article;
        }
    }
}
