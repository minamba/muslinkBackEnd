using HelpMuslimWebApp.Dal.Entities;
using HelpMuslimWebApp.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Models = HelpMuslimWebApp.Domain.Models;

namespace HelpMuslimWebApp.Dal.Repositories
{
    public class CvCoachRepository : ICvCoachRepository
    {
        private readonly HelpMuslimWebAppDatabaseContext _context;

        public CvCoachRepository(HelpMuslimWebAppDatabaseContext context)
        {
            _context = context;
        }

        public async Task<Models.CvCoachAnalysis?> GetLatestAsync(int candidateProfileId, bool byAdmin)
        {
            var entity = await _context.CvCoachAnalyses.AsNoTracking()
                .Where(a => a.CandidateProfileId == candidateProfileId && a.ByAdmin == byAdmin && a.DeletedAt == null)
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefaultAsync();
            return entity == null ? null : ToModel(entity);
        }

        public async Task<Models.CvCoachAnalysis> AddAsync(Models.CvCoachAnalysis model)
        {
            var entity = new CvCoachAnalysis
            {
                CandidateProfileId = model.CandidateProfileId,
                CreatedAt = model.CreatedAt,
                Source = model.Source,
                CvFileName = model.CvFileName,
                Result = model.Result,
                Model = model.Model,
                InputTokens = model.InputTokens,
                OutputTokens = model.OutputTokens,
                ByAdmin = model.ByAdmin,
            };
            _context.CvCoachAnalyses.Add(entity);
            await _context.SaveChangesAsync();
            return ToModel(entity);
        }

        public Task<int> DeleteCandidateAnalysesAsync(int candidateProfileId) =>
            _context.CvCoachAnalyses
                .Where(a => a.CandidateProfileId == candidateProfileId && !a.ByAdmin && a.DeletedAt == null)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(a => a.Result, string.Empty)
                    .SetProperty(a => a.CvFileName, (string?)null)
                    .SetProperty(a => a.DeletedAt, DateTime.UtcNow));

        public Task<int> CountSinceAsync(int candidateProfileId, DateTime sinceUtc) =>
            _context.CvCoachAnalyses.CountAsync(a => a.CandidateProfileId == candidateProfileId && !a.ByAdmin && a.CreatedAt >= sinceUtc);

        private static Models.CvCoachAnalysis ToModel(CvCoachAnalysis a) => new()
        {
            Id = a.Id,
            CandidateProfileId = a.CandidateProfileId,
            CreatedAt = a.CreatedAt,
            Source = a.Source,
            CvFileName = a.CvFileName,
            Result = a.Result,
            Model = a.Model,
            InputTokens = a.InputTokens,
            OutputTokens = a.OutputTokens,
            ByAdmin = a.ByAdmin,
            DeletedAt = a.DeletedAt,
        };
    }
}
