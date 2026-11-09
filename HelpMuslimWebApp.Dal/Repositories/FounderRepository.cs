using HelpMuslimWebApp.Dal.Entities;
using HelpMuslimWebApp.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Models = HelpMuslimWebApp.Domain.Models;

namespace HelpMuslimWebApp.Dal.Repositories
{
    public class FounderRepository : IFounderRepository
    {
        private const int SettingsId = 1;
        private readonly HelpMuslimWebAppDatabaseContext _context;

        public FounderRepository(HelpMuslimWebAppDatabaseContext context)
        {
            _context = context;
        }

        public async Task<int?> FindRecruiterBySirenAsync(string siren, int exceptRecruiterProfileId) =>
            await _context.RecruiterProfiles.Where(r => r.Siren == siren && r.Id != exceptRecruiterProfileId).Select(r => (int?)r.Id).FirstOrDefaultAsync();

        public async Task<int?> FindRecruiterByRnaAsync(string rna, int exceptRecruiterProfileId) =>
            await _context.RecruiterProfiles.Where(r => r.Rna == rna && r.Id != exceptRecruiterProfileId).Select(r => (int?)r.Id).FirstOrDefaultAsync();

        public Task SetIdentityAsync(int recruiterProfileId, Models.CompanyIdentity identity) =>
            _context.RecruiterProfiles.Where(r => r.Id == recruiterProfileId).ExecuteUpdateAsync(s => s
                .SetProperty(r => r.Siret, identity.Siret)
                .SetProperty(r => r.Siren, identity.Siren)
                .SetProperty(r => r.Rna, identity.Rna)
                .SetProperty(r => r.LegalName, identity.LegalName)
                .SetProperty(r => r.LegalAliases, identity.Aliases)
                .SetProperty(r => r.IdentityVerifiedAt, identity.VerifiedAt));

        public async Task<bool> SetIdentityVerifiedAsync(int recruiterProfileId, DateTime? verifiedAt) =>
            await _context.RecruiterProfiles.Where(r => r.Id == recruiterProfileId && (r.Siret != null || r.Rna != null))
                .ExecuteUpdateAsync(s => s.SetProperty(r => r.IdentityVerifiedAt, verifiedAt)) > 0;

        public Task SetEmailConfirmedAsync(int recruiterProfileId, bool confirmed) =>
            _context.RecruiterProfiles.Where(r => r.Id == recruiterProfileId && r.IsEmailConfirmed != confirmed)
                .ExecuteUpdateAsync(s => s.SetProperty(r => r.IsEmailConfirmed, confirmed));

        public Task SetSignupIpHashAsync(int recruiterProfileId, string hash) =>
            _context.RecruiterProfiles.Where(r => r.Id == recruiterProfileId && r.SignupIpHash == null)
                .ExecuteUpdateAsync(s => s.SetProperty(r => r.SignupIpHash, hash));

        public Task SetRevokedAsync(int recruiterProfileId, DateTime? revokedAt) =>
            _context.RecruiterProfiles.Where(r => r.Id == recruiterProfileId).ExecuteUpdateAsync(s => s.SetProperty(r => r.FounderRevokedAt, revokedAt));

        public Task<int> CountFoundersAsync() => _context.RecruiterProfiles.CountAsync(r => r.IsFounder == true);

        public Task<bool> HasPublishedOfferAsync(int recruiterProfileId) => _context.JobOffers.AnyAsync(o => o.RecruiterProfileId == recruiterProfileId);

        public async Task<List<Models.FounderReview>> GetReviewAsync() =>
            await _context.RecruiterProfiles.AsNoTracking()
                .Where(r => r.IsFounder == true || r.Siret != null || r.Rna != null || r.FounderRevokedAt != null)
                .OrderByDescending(r => r.IsFounder).ThenBy(r => r.FounderSince).ThenBy(r => r.CreatedAt)
                .Select(r => new Models.FounderReview
                {
                    RecruiterProfileId = r.Id,
                    CompanyName = r.CompanyName,
                    FirstName = r.FirstName,
                    LastName = r.LastName,
                    Email = r.Email,
                    IsFounder = r.IsFounder == true,
                    FounderSince = r.FounderSince,
                    FounderRevokedAt = r.FounderRevokedAt,
                    Siret = r.Siret,
                    Rna = r.Rna,
                    LegalName = r.LegalName,
                    IdentityVerifiedAt = r.IdentityVerifiedAt,
                    IsEmailConfirmed = r.IsEmailConfirmed == true,
                    SignupIpHash = r.SignupIpHash,
                    FirstOfferAt = r.Offers.Min(o => o.CreatedAt),
                    OffersCount = r.Offers.Count,
                    IsDisabled = r.IsDisabled == true,
                    CreatedAt = r.CreatedAt,
                })
                .ToListAsync();

        public Task UpdateLaunchAsync(bool enabled, int slots, bool autoGrant) =>
            _context.PremiumSettings.Where(s => s.Id == SettingsId).ExecuteUpdateAsync(s => s
                .SetProperty(p => p.LaunchOfferEnabled, enabled)
                .SetProperty(p => p.FounderSlots, slots)
                .SetProperty(p => p.FounderAutoGrant, autoGrant)
                .SetProperty(p => p.UpdatedAt, DateTime.UtcNow));
    }
}
