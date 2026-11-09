using HelpMuslimWebApp.Dal.Entities;
using HelpMuslimWebApp.Domain.Models;
using HelpMuslimWebApp.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace HelpMuslimWebApp.Dal.Repositories
{
    public class SiteStatsRepository : ISiteStatsRepository
    {
        private readonly HelpMuslimWebAppDatabaseContext _context;

        public SiteStatsRepository(HelpMuslimWebAppDatabaseContext context)
        {
            _context = context;
        }

        public async Task<bool> RecordVisitAsync(string visitorId, string? countryCode, string? source, DateTime nowUtc)
        {
            // Un rechargement ou un appel rejoué en boucle ne fait pas grossir la table
            var hourAgo = nowUtc.AddHours(-1);
            if (await _context.SiteVisits.AnyAsync(v => v.VisitorId == visitorId && v.CreatedAt > hourAgo)) return false;

            _context.SiteVisits.Add(new SiteVisit { VisitorId = visitorId, CountryCode = countryCode, Source = source, CreatedAt = nowUtc });
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<SiteVisitRow>> GetVisitsAsync(DateTime fromUtc, DateTime toUtc) =>
            await _context.SiteVisits.AsNoTracking()
                .Where(v => v.CreatedAt >= fromUtc && v.CreatedAt < toUtc)
                .Select(v => new SiteVisitRow(v.VisitorId, v.CountryCode, v.CreatedAt, v.Source))
                .ToListAsync();

        public async Task<List<DateTime>> GetCandidateSignupsAsync(DateTime fromUtc, DateTime toUtc) =>
            await _context.CandidateProfiles.AsNoTracking()
                .Where(p => p.CreatedAt >= fromUtc && p.CreatedAt < toUtc)
                .Select(p => p.CreatedAt!.Value).ToListAsync();

        public async Task<List<DateTime>> GetRecruiterSignupsAsync(DateTime fromUtc, DateTime toUtc) =>
            await _context.RecruiterProfiles.AsNoTracking()
                .Where(p => p.CreatedAt >= fromUtc && p.CreatedAt < toUtc)
                .Select(p => p.CreatedAt!.Value).ToListAsync();

        public async Task<List<DateTime>> GetApplicationsAsync(DateTime fromUtc, DateTime toUtc) =>
            await _context.JobApplications.AsNoTracking()
                .Where(a => a.CreatedAt >= fromUtc && a.CreatedAt < toUtc)
                .Select(a => a.CreatedAt!.Value).ToListAsync();

        public async Task<List<PublishedOfferRow>> GetPublishedOffersAsync(DateTime fromUtc, DateTime toUtc) =>
            await _context.JobOffers.AsNoTracking()
                .Where(o => o.CreatedAt >= fromUtc && o.CreatedAt < toUtc && !o.RecruiterProfile!.IsTestAccount)
                .Select(o => new PublishedOfferRow(
                    o.CreatedAt!.Value,
                    o.Applications.Where(a => !a.CandidateProfile!.IsTestAccount).Min(a => a.CreatedAt),
                    o.Applications.Count(a => !a.CandidateProfile!.IsTestAccount)))
                .ToListAsync();

        public async Task<int> CountActiveRecruitersAsync(DateTime fromUtc, DateTime toUtc)
        {
            var published = await _context.JobOffers.AsNoTracking()
                .Where(o => o.CreatedAt >= fromUtc && o.CreatedAt < toUtc && !o.RecruiterProfile!.IsTestAccount)
                .Select(o => o.RecruiterProfileId!.Value)
                .ToListAsync();
            var wrote = await _context.Conversations.AsNoTracking()
                .Where(c => !c.RecruiterProfile!.IsTestAccount
                            && c.Messages.Any(m => m.SentAt >= fromUtc && m.SentAt < toUtc && m.SenderUserId == c.RecruiterProfile!.UserId))
                .Select(c => c.RecruiterProfileId!.Value)
                .ToListAsync();
            return published.Concat(wrote).Distinct().Count();
        }

        public async Task<int> CountActiveCandidatesAsync(DateTime fromUtc, DateTime toUtc) =>
            await _context.JobApplications.AsNoTracking()
                .Where(a => a.CreatedAt >= fromUtc && a.CreatedAt < toUtc && !a.CandidateProfile!.IsTestAccount)
                .Select(a => a.CandidateProfileId)
                .Distinct()
                .CountAsync();

        public async Task<List<FilledOfferRow>> GetFilledOffersAsync(DateTime fromUtc, DateTime toUtc) =>
            await _context.OfferFills.AsNoTracking()
                .Where(f => f.FilledAt >= fromUtc && f.FilledAt < toUtc)
                .OrderByDescending(f => f.FilledAt)
                .Select(f => new FilledOfferRow(f.JobOfferId, f.Title, f.CompanyName, f.City, f.ContractType, f.FilledAt!.Value))
                .ToListAsync();

        public async Task<List<SubscriptionEventRow>> GetSubscriptionEventsAsync(string audience, DateTime toUtc) =>
            await _context.SubscriptionEvents.AsNoTracking()
                .Where(e => e.Audience == audience && e.OccurredAt < toUtc)
                .OrderBy(e => e.OccurredAt).ThenBy(e => e.Id)
                .Select(e => new SubscriptionEventRow(e.ProfileId, e.Kind, e.Plan, e.GrantedUntil, e.OccurredAt, e.Backfilled))
                .ToListAsync();

        public async Task<List<SubscriptionProfileRow>> GetSubscriptionProfilesAsync(string audience) =>
            audience == SubscriptionAudiences.Recruiter
                ? await _context.RecruiterProfiles.AsNoTracking()
                    .Select(r => new SubscriptionProfileRow(r.Id, r.CompanyName ?? ((r.FirstName ?? "") + " " + (r.LastName ?? "")).Trim(), r.CreatedAt))
                    .ToListAsync()
                : await _context.CandidateProfiles.AsNoTracking()
                    .Select(c => new SubscriptionProfileRow(c.Id, ((c.FirstName ?? "") + " " + (c.LastName ?? "")).Trim(), c.CreatedAt))
                    .ToListAsync();

        public async Task<DateTime?> GetSubscriptionTrackedSinceAsync() =>
            await _context.SubscriptionEvents.AsNoTracking().MinAsync(e => (DateTime?)e.OccurredAt);

        public async Task<List<CoachUsageRow>> GetCoachUsageAsync(DateTime fromUtc, DateTime toUtc)
        {
            var rows = await _context.CvCoachAnalyses.AsNoTracking()
                .Where(a => a.CreatedAt >= fromUtc && a.CreatedAt < toUtc)
                .Select(a => new
                {
                    a.CandidateProfileId,
                    a.CandidateProfile!.FirstName,
                    a.CandidateProfile.LastName,
                    a.CandidateProfile.Email,
                    a.CandidateProfile.PremiumGrantedUntil,
                    a.CandidateProfile.PremiumPaidUntil,
                    a.ByAdmin,
                    a.Model,
                    a.InputTokens,
                    a.OutputTokens,
                    a.CreatedAt,
                })
                .ToListAsync();
            return rows.Select(r => new CoachUsageRow(r.CandidateProfileId, $"{r.FirstName} {r.LastName}".Trim(), r.Email,
                Premium.Until(r.PremiumGrantedUntil, r.PremiumPaidUntil), r.ByAdmin, r.Model, r.InputTokens, r.OutputTokens, r.CreatedAt)).ToList();
        }

        public async Task<List<RecruiterAiUsageRow>> GetRecruiterAiUsageAsync(DateTime fromUtc, DateTime toUtc)
        {
            var rows = await _context.RecruiterAiUsages.AsNoTracking()
                .Where(u => u.CreatedAt >= fromUtc && u.CreatedAt < toUtc)
                .Select(u => new
                {
                    u.RecruiterProfileId,
                    u.RecruiterProfile!.CompanyName,
                    u.RecruiterProfile.FirstName,
                    u.RecruiterProfile.LastName,
                    u.RecruiterProfile.Email,
                    u.RecruiterProfile.ProGrantedUntil,
                    u.RecruiterProfile.ProPaidUntil,
                    u.Feature,
                    u.Model,
                    u.InputTokens,
                    u.OutputTokens,
                    u.CreatedAt,
                })
                .ToListAsync();
            return rows.Select(r => new RecruiterAiUsageRow(r.RecruiterProfileId, r.CompanyName, $"{r.FirstName} {r.LastName}".Trim(), r.Email,
                Premium.Until(r.ProGrantedUntil, r.ProPaidUntil), r.Feature, r.Model, r.InputTokens, r.OutputTokens, r.CreatedAt)).ToList();
        }
    }
}
