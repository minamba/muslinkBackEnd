using HelpMuslimWebApp.Domain.Models;

namespace HelpMuslimWebApp.Domain.Repositories
{
    public interface IArticleRepository
    {
        // Liste sans le contenu complet (allégée)
        Task<PagedResult<Article>> GetArticlesAsync(ArticleFilter filter);
        Task<IEnumerable<string>> GetCategoriesAsync();
        Task<Article?> GetArticleByIdAsync(int id);
        Task<Article> AddArticleAsync(Article model);
        Task<Article?> UpdateArticleAsync(Article model);
        Task<bool> DeleteArticleAsync(int id);
        // Textes où les articles référencent leurs fichiers (contenu, couverture, vignette), sauf un article éventuel
        Task<IEnumerable<string>> GetMediaSourcesAsync(int? exceptArticleId = null);
    }
}
