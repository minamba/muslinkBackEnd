using HelpMuslimWebApp.Domain.Models;

namespace HelpMuslimWebApp.Domain.Services
{
    public interface IArticleService
    {
        Task<PagedResult<Article>> GetArticlesAsync(ArticleFilter filter);
        Task<IEnumerable<string>> GetCategoriesAsync();
        Task<Article?> GetArticleByIdAsync(int id);
        Task<Article> AddArticleAsync(Article model);
        Task<Article?> UpdateArticleAsync(Article model);
        Task<bool> DeleteArticleAsync(int id);
        // Textes où les articles référencent leurs fichiers (nettoyage du disque)
        Task<IEnumerable<string>> GetMediaSourcesAsync(int? exceptArticleId = null);
    }
}
