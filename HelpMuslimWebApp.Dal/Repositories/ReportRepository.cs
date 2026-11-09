using HelpMuslimWebApp.Dal.Entities;
using HelpMuslimWebApp.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Models = HelpMuslimWebApp.Domain.Models;

namespace HelpMuslimWebApp.Dal.Repositories
{
    public class ReportRepository : IReportRepository
    {
        private readonly HelpMuslimWebAppDatabaseContext _context;

        public ReportRepository(HelpMuslimWebAppDatabaseContext context)
        {
            _context = context;
        }

        public async Task<Models.Report> AddReportAsync(Models.Report model)
        {
            var entity = new Report
            {
                TargetType = model.TargetType,
                TargetId = model.TargetId,
                TargetLabel = model.TargetLabel,
                AuthorUserId = model.AuthorUserId,
                AuthorKind = model.AuthorKind,
                AuthorCandidateProfileId = model.AuthorCandidateProfileId,
                AuthorRecruiterProfileId = model.AuthorRecruiterProfileId,
                AuthorName = model.AuthorName,
                AuthorEmail = model.AuthorEmail,
                Reason = model.Reason,
                Justification = model.Justification,
                Status = model.Status,
                CreatedAt = model.CreatedAt,
            };
            _context.Reports.Add(entity);
            await _context.SaveChangesAsync();
            model.Id = entity.Id;
            return model;
        }

        public Task<Models.Report?> GetReportAsync(int id) =>
            _context.Reports.AsNoTracking().Where(r => r.Id == id).Select(Projection).FirstOrDefaultAsync();

        public Task<List<Models.Report>> GetReportsAsync(string? targetType) =>
            _context.Reports.AsNoTracking()
                .Where(r => targetType == null || r.TargetType == targetType)
                .OrderByDescending(r => r.CreatedAt)
                .Select(Projection)
                .ToListAsync();

        public Task<Dictionary<string, int>> CountNewByTypeAsync() =>
            _context.Reports.AsNoTracking()
                .Where(r => r.Status == Models.ReportStatuses.New)
                .GroupBy(r => r.TargetType)
                .ToDictionaryAsync(g => g.Key, g => g.Count());

        public Task<bool> HasOpenReportAsync(string authorUserId, string targetType, int targetId) =>
            _context.Reports.AnyAsync(r => r.AuthorUserId == authorUserId && r.TargetType == targetType && r.TargetId == targetId
                                          && (r.Status == Models.ReportStatuses.New || r.Status == Models.ReportStatuses.InReview));

        public Task<int> CountReportsSinceAsync(string authorUserId, DateTime sinceUtc) =>
            _context.Reports.CountAsync(r => r.AuthorUserId == authorUserId && r.CreatedAt >= sinceUtc);

        public async Task<Models.Report?> SetStatusAsync(int id, string status, string? adminNote, string? handledBy)
        {
            var entity = await _context.Reports.FirstOrDefaultAsync(r => r.Id == id);
            if (entity == null) return null;
            entity.Status = status;
            entity.AdminNote = adminNote;
            entity.HandledBy = handledBy;
            entity.StatusChangedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return await GetReportAsync(id);
        }

        public async Task<Models.Report?> UpdateReportAsync(int id, string reason, string justification, string? adminNote)
        {
            var entity = await _context.Reports.FirstOrDefaultAsync(r => r.Id == id);
            if (entity == null) return null;
            entity.Reason = reason;
            entity.Justification = justification;
            entity.AdminNote = adminNote;
            await _context.SaveChangesAsync();
            return await GetReportAsync(id);
        }

        public async Task<bool> DeleteReportAsync(int id) =>
            await _context.Reports.Where(r => r.Id == id).ExecuteDeleteAsync() > 0;

        private static readonly System.Linq.Expressions.Expression<Func<Report, Models.Report>> Projection = r => new Models.Report
        {
            Id = r.Id,
            TargetType = r.TargetType,
            TargetId = r.TargetId,
            TargetLabel = r.TargetLabel,
            AuthorUserId = r.AuthorUserId,
            AuthorKind = r.AuthorKind,
            AuthorCandidateProfileId = r.AuthorCandidateProfileId,
            AuthorRecruiterProfileId = r.AuthorRecruiterProfileId,
            AuthorName = r.AuthorName,
            AuthorEmail = r.AuthorEmail,
            Reason = r.Reason,
            Justification = r.Justification,
            Status = r.Status,
            AdminNote = r.AdminNote,
            HandledBy = r.HandledBy,
            CreatedAt = r.CreatedAt,
            StatusChangedAt = r.StatusChangedAt,
        };
    }
}
