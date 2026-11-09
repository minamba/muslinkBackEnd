using HelpMuslimWebApp.Dal.Entities;
using HelpMuslimWebApp.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Models = HelpMuslimWebApp.Domain.Models;

namespace HelpMuslimWebApp.Dal.Repositories
{
    public class RecruiterAiRepository : IRecruiterAiRepository
    {
        private readonly HelpMuslimWebAppDatabaseContext _context;

        public RecruiterAiRepository(HelpMuslimWebAppDatabaseContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Models.RecruiterAiUsage model)
        {
            _context.RecruiterAiUsages.Add(new RecruiterAiUsage
            {
                RecruiterProfileId = model.RecruiterProfileId,
                CreatedAt = model.CreatedAt,
                Feature = model.Feature,
                Model = model.Model,
                InputTokens = model.InputTokens,
                OutputTokens = model.OutputTokens,
            });
            await _context.SaveChangesAsync();
        }

        public Task<int> CountSinceAsync(int recruiterProfileId, IReadOnlyCollection<string> features, DateTime sinceUtc) =>
            _context.RecruiterAiUsages.CountAsync(u => u.RecruiterProfileId == recruiterProfileId && features.Contains(u.Feature) && u.CreatedAt >= sinceUtc);
    }
}
