using System.Security.Claims;
using HelpMuslimWebApp.Api.Request;
using HelpMuslimWebApp.Api.ViewModels;

namespace HelpMuslimWebApp.Api.Builders
{
    public interface IArticleViewModelBuilder
    {
        Task<PagedViewModel<ArticleViewModel>> GetArticlesAsync(ArticleQueryRequest query);
        Task<IEnumerable<string>> GetCategoriesAsync();
        Task<ArticleViewModel?> GetArticleByIdAsync(int id);
        Task<ArticleViewModel> AddArticleAsync(ClaimsPrincipal user, ArticleRequest model);
        Task<ArticleViewModel?> UpdateArticleAsync(ClaimsPrincipal user, ArticleRequest model);
        Task<bool> DeleteArticleAsync(int id);
        Task<ArticleMediaViewModel> UploadMediaAsync(IFormFile file);
    }
}
