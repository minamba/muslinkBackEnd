using System.Security.Cryptography;
using HelpMuslimWebApp.Dal.Entities;
using HelpMuslimWebApp.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Models = HelpMuslimWebApp.Domain.Models;

namespace HelpMuslimWebApp.Dal.Repositories
{
    public class MatchAlertRepository : IMatchAlertRepository
    {
        private readonly HelpMuslimWebAppDatabaseContext _context;

        public MatchAlertRepository(HelpMuslimWebAppDatabaseContext context)
        {
            _context = context;
        }

        public async Task<HashSet<(int JobOfferId, int CandidateProfileId)>> GetAlertedPairsAsync() =>
            (await _context.CandidateMatchAlerts.AsNoTracking()
                .Select(a => new { a.JobOfferId, a.CandidateProfileId })
                .ToListAsync())
            .Select(a => (a.JobOfferId!.Value, a.CandidateProfileId!.Value))
            .ToHashSet();

        public async Task<HashSet<(int JobOfferId, int CandidateProfileId)>> GetApplicationPairsAsync() =>
            (await _context.JobApplications.AsNoTracking()
                .Where(a => a.JobOfferId != null && a.CandidateProfileId != null)
                .Select(a => new { a.JobOfferId, a.CandidateProfileId })
                .ToListAsync())
            .Select(a => (a.JobOfferId!.Value, a.CandidateProfileId!.Value))
            .ToHashSet();

        public async Task AddAlertsAsync(IReadOnlyCollection<Models.MatchAlertPair> pairs)
        {
            var now = DateTime.UtcNow;
            _context.CandidateMatchAlerts.AddRange(pairs.Select(p => new CandidateMatchAlert
            {
                JobOfferId = p.JobOfferId, CandidateProfileId = p.CandidateProfileId, Score = p.Score, CreatedAt = now,
            }));
            await _context.SaveChangesAsync();
        }

        public async Task<string> GetOrCreateTokenAsync(int recruiterProfileId)
        {
            var existing = await _context.RecruiterProfiles.Where(r => r.Id == recruiterProfileId).Select(r => r.MatchAlertsToken).FirstOrDefaultAsync();
            if (!string.IsNullOrEmpty(existing)) return existing;

            var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant();
            await _context.RecruiterProfiles.Where(r => r.Id == recruiterProfileId)
                .ExecuteUpdateAsync(s => s.SetProperty(r => r.MatchAlertsToken, token));
            return token;
        }

        public async Task<bool> SetEnabledAsync(int recruiterProfileId, bool enabled) =>
            await _context.RecruiterProfiles.Where(r => r.Id == recruiterProfileId)
                .ExecuteUpdateAsync(s => s.SetProperty(r => r.MatchAlertsDisabled, !enabled)) > 0;

        public async Task<bool> SetEnabledByTokenAsync(string token, bool enabled) =>
            await _context.RecruiterProfiles.Where(r => r.MatchAlertsToken == token)
                .ExecuteUpdateAsync(s => s.SetProperty(r => r.MatchAlertsDisabled, !enabled)) > 0;

        public async Task<bool> SetMessageAlertsEnabledAsync(int recruiterProfileId, bool enabled) =>
            await _context.RecruiterProfiles.Where(r => r.Id == recruiterProfileId)
                .ExecuteUpdateAsync(s => s.SetProperty(r => r.MessageAlertsDisabled, !enabled)) > 0;

        public async Task<bool> SetMessageAlertsEnabledByTokenAsync(string token, bool enabled) =>
            await _context.RecruiterProfiles.Where(r => r.MatchAlertsToken == token)
                .ExecuteUpdateAsync(s => s.SetProperty(r => r.MessageAlertsDisabled, !enabled)) > 0;

        public async Task<bool> SetApplicationAlertsEnabledAsync(int recruiterProfileId, bool enabled) =>
            await _context.RecruiterProfiles.Where(r => r.Id == recruiterProfileId)
                .ExecuteUpdateAsync(s => s.SetProperty(r => r.ApplicationAlertsDisabled, !enabled)) > 0;

        public async Task<bool> SetApplicationAlertsEnabledByTokenAsync(string token, bool enabled) =>
            await _context.RecruiterProfiles.Where(r => r.MatchAlertsToken == token)
                .ExecuteUpdateAsync(s => s.SetProperty(r => r.ApplicationAlertsDisabled, !enabled)) > 0;
    }
}
