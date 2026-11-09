using HelpMuslimWebApp.Domain.Models;
using HelpMuslimWebApp.Domain.Repositories;

namespace HelpMuslimWebApp.Domain.Services.impl
{
    public class ArticleService : IArticleService
    {
        private readonly IArticleRepository _repository;

        public ArticleService(IArticleRepository repository)
        {
            _repository = repository;
        }

        public Task<PagedResult<Article>> GetArticlesAsync(ArticleFilter filter)
        {
            filter.Page = Math.Max(1, filter.Page);
            filter.PageSize = Math.Clamp(filter.PageSize, 1, 30);
            return _repository.GetArticlesAsync(filter);
        }

        public Task<IEnumerable<string>> GetCategoriesAsync() => _repository.GetCategoriesAsync();
        public Task<Article?> GetArticleByIdAsync(int id) => _repository.GetArticleByIdAsync(id);

        public Task<Article> AddArticleAsync(Article model)
        {
            model.PublishedAt = DateTime.UtcNow;
            model.UpdatedAt = null;
            return _repository.AddArticleAsync(model);
        }

        public Task<Article?> UpdateArticleAsync(Article model)
        {
            model.UpdatedAt = DateTime.UtcNow;
            return _repository.UpdateArticleAsync(model);
        }

        public Task<bool> DeleteArticleAsync(int id) => _repository.DeleteArticleAsync(id);
        public Task<IEnumerable<string>> GetMediaSourcesAsync(int? exceptArticleId = null) => _repository.GetMediaSourcesAsync(exceptArticleId);
    }
}
