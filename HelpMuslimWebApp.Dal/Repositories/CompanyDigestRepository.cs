using HelpMuslimWebApp.Dal.Entities;
using HelpMuslimWebApp.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Models = HelpMuslimWebApp.Domain.Models;

namespace HelpMuslimWebApp.Dal.Repositories
{
    public class CompanyDigestRepository : ICompanyDigestRepository
    {
        private readonly HelpMuslimWebAppDatabaseContext _context;

        public CompanyDigestRepository(HelpMuslimWebAppDatabaseContext context)
        {
            _context = context;
        }

        public async Task AddPendingAsync(int jobOfferId, IReadOnlyCollection<int> candidateProfileIds)
        {
            var existing = await _context.CompanyOfferDigests
                .Where(d => d.JobOfferId == jobOfferId && candidateProfileIds.Contains(d.CandidateProfileId!.Value))
                .Select(d => d.CandidateProfileId!.Value)
                .ToListAsync();
            var now = DateTime.UtcNow;
            _context.CompanyOfferDigests.AddRange(candidateProfileIds.Except(existing).Distinct().Select(id => new CompanyOfferDigest
            {
                JobOfferId = jobOfferId, CandidateProfileId = id, CreatedAt = now,
            }));
            await _context.SaveChangesAsync();
        }

        public Task<List<Models.CompanyDigestItem>> GetPendingAsync() =>
            _context.CompanyOfferDigests.AsNoTracking()
                .Where(d => d.SentAt == null)
                .OrderBy(d => d.CreatedAt)
                .Select(d => new Models.CompanyDigestItem
                {
                    Id = d.Id,
                    CandidateProfileId = d.CandidateProfileId!.Value,
                    CandidateEmail = d.CandidateProfile!.Email,
                    CandidateFirstName = d.CandidateProfile.FirstName,
                    JobOfferId = d.JobOfferId!.Value,
                    OfferTitle = d.JobOffer!.Title,
                    ContractType = d.JobOffer.ContractType,
                    City = d.JobOffer.City,
                    CompanyId = d.JobOffer.RecruiterProfileId ?? 0,
                    CompanyName = d.JobOffer.RecruiterProfile!.CompanyName,
                    OfferStillOpen = d.JobOffer.IsOpen == true && d.JobOffer.IsFilled != true && d.JobOffer.IsHidden != true
                                     && d.JobOffer.RecruiterProfile.IsDisabled != true,
                    StillFollowing = _context.CompanyFollows.Any(f => f.CandidateProfileId == d.CandidateProfileId && f.RecruiterProfileId == d.JobOffer.RecruiterProfileId),
                    AlreadyApplied = _context.JobApplications.Any(a => a.CandidateProfileId == d.CandidateProfileId && a.JobOfferId == d.JobOfferId),
                })
                .ToListAsync();

        public async Task MarkSentAsync(IReadOnlyCollection<int> ids)
        {
            var now = DateTime.UtcNow;
            await _context.CompanyOfferDigests.Where(d => ids.Contains(d.Id))
                .ExecuteUpdateAsync(s => s.SetProperty(d => d.SentAt, now));
        }
    }
}
