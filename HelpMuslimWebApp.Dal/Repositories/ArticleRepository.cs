using HelpMuslimWebApp.Dal.Entities;
using HelpMuslimWebApp.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Models = HelpMuslimWebApp.Domain.Models;

namespace HelpMuslimWebApp.Dal.Repositories
{
    public class ArticleRepository : IArticleRepository
    {
        private readonly HelpMuslimWebAppDatabaseContext _context;

        public ArticleRepository(HelpMuslimWebAppDatabaseContext context)
        {
            _context = context;
        }

        public async Task<Models.PagedResult<Models.Article>> GetArticlesAsync(Models.ArticleFilter filter)
        {
            var query = _context.Articles.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(filter.Category)) query = query.Where(a => a.Category == filter.Category);
            if (filter.ExcludeId.HasValue) query = query.Where(a => a.Id != filter.ExcludeId);
            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var pattern = Models.SearchText.Pattern(filter.Search);
                query = query.Where(a =>
                    EF.Functions.ILike(EF.Functions.Unaccent(a.Title!), pattern) ||
                    EF.Functions.ILike(EF.Functions.Unaccent(a.Summary!), pattern));
            }

            var total = await query.CountAsync();
            var ordered = filter.OldestFirst
                ? query.OrderBy(a => a.PublishedAt).ThenBy(a => a.Id)
                : query.OrderByDescending(a => a.PublishedAt).ThenByDescending(a => a.Id);
            var items = await ordered
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                // Sans le contenu HTML : inutile (et lourd) pour les cartes
                .Select(a => new Models.Article
                {
                    Id = a.Id,
                    Title = a.Title,
                    Summary = a.Summary,
                    Excerpt = a.Excerpt ?? a.Summary,
                    Category = a.Category,
                    CoverImagePath = a.CoverImagePath,
                    ThumbnailPath = a.ThumbnailPath,
                    ReadingMinutes = a.ReadingMinutes ?? 1,
                    AuthorUserId = a.AuthorUserId,
                    AuthorName = a.AuthorName,
                    PublishedAt = a.PublishedAt,
                    UpdatedAt = a.UpdatedAt,
                })
                .ToListAsync();

            return new Models.PagedResult<Models.Article> { Items = items, Total = total, Page = filter.Page, PageSize = filter.PageSize };
        }

        public async Task<IEnumerable<string>> GetCategoriesAsync()
        {
            return await _context.Articles.AsNoTracking()
                .Where(a => a.Category != null && a.Category != "")
                .Select(a => a.Category!)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();
        }

        public async Task<Models.Article?> GetArticleByIdAsync(int id)
        {
            var entity = await _context.Articles.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id);
            return entity == null ? null : ToModel(entity);
        }

        public async Task<Models.Article> AddArticleAsync(Models.Article model)
        {
            var entity = new Article
            {
                AuthorUserId = model.AuthorUserId,
                AuthorName = model.AuthorName,
                PublishedAt = model.PublishedAt ?? DateTime.UtcNow,
            };
            Apply(entity, model);
            _context.Articles.Add(entity);
            await _context.SaveChangesAsync();
            return ToModel(entity);
        }

        public async Task<Models.Article?> UpdateArticleAsync(Models.Article model)
        {
            var entity = await _context.Articles.FirstOrDefaultAsync(a => a.Id == model.Id);
            if (entity == null) return null;

            Apply(entity, model);
            entity.UpdatedByName = model.UpdatedByName;
            entity.UpdatedAt = model.UpdatedAt;
            await _context.SaveChangesAsync();
            return ToModel(entity);
        }

        public async Task<IEnumerable<string>> GetMediaSourcesAsync(int? exceptArticleId = null) =>
            await _context.Articles.AsNoTracking()
                .Where(a => exceptArticleId == null || a.Id != exceptArticleId)
                .Select(a => (a.Content ?? "") + " " + (a.CoverImagePath ?? "") + " " + (a.ThumbnailPath ?? ""))
                .ToListAsync();

        public async Task<bool> DeleteArticleAsync(int id)
        {
            var entity = await _context.Articles.FirstOrDefaultAsync(a => a.Id == id);
            if (entity == null) return false;

            _context.Articles.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        private static void Apply(Article entity, Models.Article model)
        {
            if (model.Title != null) entity.Title = model.Title.Trim();
            if (model.Content != null) entity.Content = model.Content;
            entity.Summary = model.Summary;
            entity.Excerpt = model.Excerpt;
            entity.Category = model.Category;
            entity.CoverImagePath = model.CoverImagePath;
            entity.ThumbnailPath = model.ThumbnailPath;
            entity.ReadingMinutes = model.ReadingMinutes;
        }

        private static Models.Article ToModel(Article entity) => new()
        {
            Id = entity.Id,
            Title = entity.Title,
            Summary = entity.Summary,
            Excerpt = entity.Excerpt ?? entity.Summary,
            Content = entity.Content,
            Category = entity.Category,
            CoverImagePath = entity.CoverImagePath,
            ThumbnailPath = entity.ThumbnailPath,
            ReadingMinutes = entity.ReadingMinutes ?? 1,
            AuthorUserId = entity.AuthorUserId,
            AuthorName = entity.AuthorName,
            UpdatedByName = entity.UpdatedByName,
            PublishedAt = entity.PublishedAt,
            UpdatedAt = entity.UpdatedAt,
        };
    }
}
