using HelpMuslimWebApp.Dal.Entities;
using HelpMuslimWebApp.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Models = HelpMuslimWebApp.Domain.Models;

namespace HelpMuslimWebApp.Dal.Repositories
{
    public class RecruiterProfileRepository : IRecruiterProfileRepository
    {
        private readonly HelpMuslimWebAppDatabaseContext _context;

        public RecruiterProfileRepository(HelpMuslimWebAppDatabaseContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Models.RecruiterProfile>> GetRecruiterProfilesAsync(string? search = null)
        {
            var query = _context.RecruiterProfiles.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var pattern = Models.SearchText.Pattern(search);
                query = query.Where(r =>
                    EF.Functions.ILike(EF.Functions.Unaccent(r.FirstName!), pattern) ||
                    EF.Functions.ILike(EF.Functions.Unaccent(r.LastName!), pattern) ||
                    EF.Functions.ILike(r.Email!, pattern) ||
                    EF.Functions.ILike(EF.Functions.Unaccent(r.CompanyName!), pattern));
            }

            return await query.OrderByDescending(r => r.CreatedAt).Select(Projection()).ToListAsync();
        }

        // ---------- Annuaire des entreprises ----------

        public async Task<IEnumerable<Models.Company>> GetCompaniesAsync(string? search, string? sector, int take, bool includeDisabled = false, bool includeTestAccounts = false)
        {
            var query = _context.RecruiterProfiles.AsNoTracking().Where(r => r.CompanyName != null && r.CompanyName != "");
            // Annuaire public : les entreprises désactivées n'y figurent pas (l'administration les voit toutes)
            if (!includeDisabled) query = query.Where(r => r.IsDisabled != true);
            // Recruteurs de test : seulement pour les comptes de test et l'administration
            if (!includeTestAccounts) query = query.Where(r => !r.IsTestAccount);
            if (!string.IsNullOrWhiteSpace(sector))
            {
                // Activité exacte, sans tenir compte de la casse ni des espaces autour (saisie libre du recruteur)
                var normalized = sector.Trim().ToLower();
                query = query.Where(r => r.CompanySector != null && r.CompanySector.Trim().ToLower() == normalized);
            }
            if (!string.IsNullOrWhiteSpace(search))
            {
                var pattern = Models.SearchText.Pattern(search);
                query = query.Where(r =>
                    EF.Functions.ILike(EF.Functions.Unaccent(r.CompanyName!), pattern) ||
                    EF.Functions.ILike(EF.Functions.Unaccent(r.CompanySector!), pattern) ||
                    EF.Functions.ILike(EF.Functions.Unaccent(r.City!), pattern));
            }

            return await query.Select(CompanyProjection)
                .OrderByDescending(c => c.OpenOffersCount).ThenBy(c => c.CompanyName)
                .Take(take)
                .ToListAsync();
        }

        public async Task<IEnumerable<Models.CompanySectorCount>> GetCompanySectorsAsync()
        {
            var sectors = await _context.RecruiterProfiles.AsNoTracking()
                .Where(r => r.IsDisabled != true && !r.IsTestAccount && r.CompanyName != null && r.CompanyName != "" && r.CompanySector != null && r.CompanySector.Trim() != "")
                .Select(r => r.CompanySector!.Trim())
                .ToListAsync();

            // Regroupement insensible à la casse, libellé le plus fréquent conservé
            return sectors
                .GroupBy(s => s.ToLowerInvariant())
                .Select(g => new Models.CompanySectorCount
                {
                    Sector = g.GroupBy(s => s).OrderByDescending(v => v.Count()).First().Key,
                    Count = g.Count(),
                })
                .OrderBy(s => s.Sector, StringComparer.Create(new System.Globalization.CultureInfo("fr-FR"), true))
                .ToList();
        }

        public async Task<Models.Company?> GetCompanyAsync(int id) =>
            await _context.RecruiterProfiles.AsNoTracking()
                .Where(r => r.Id == id && r.CompanyName != null && r.CompanyName != "")
                .Select(CompanyProjection)
                .FirstOrDefaultAsync();

        // ---------- Entreprises suivies par les candidats ----------

        public async Task<IEnumerable<Models.Company>> GetFollowedCompaniesAsync(int candidateProfileId, bool includeTestAccounts) =>
            await _context.CompanyFollows.AsNoTracking()
                .Where(f => f.CandidateProfileId == candidateProfileId && f.RecruiterProfile!.IsDisabled != true
                            && (includeTestAccounts || !f.RecruiterProfile.IsTestAccount))
                .OrderByDescending(f => f.CreatedAt)
                .Select(f => f.RecruiterProfile!)
                .Select(CompanyProjection)
                .ToListAsync();

        public async Task FollowCompanyAsync(int candidateProfileId, int recruiterProfileId)
        {
            if (await _context.CompanyFollows.AnyAsync(f => f.CandidateProfileId == candidateProfileId && f.RecruiterProfileId == recruiterProfileId)) return;
            _context.CompanyFollows.Add(new CompanyFollow { CandidateProfileId = candidateProfileId, RecruiterProfileId = recruiterProfileId, CreatedAt = DateTime.UtcNow });
            await _context.SaveChangesAsync();
        }

        public async Task<bool> UnfollowCompanyAsync(int candidateProfileId, int recruiterProfileId) =>
            await _context.CompanyFollows
                .Where(f => f.CandidateProfileId == candidateProfileId && f.RecruiterProfileId == recruiterProfileId)
                .ExecuteDeleteAsync() > 0;

        public async Task<IEnumerable<Models.CompanyFollower>> GetCompanyFollowersAsync(int recruiterProfileId) =>
            await _context.CompanyFollows.AsNoTracking()
                .Where(f => f.RecruiterProfileId == recruiterProfileId)
                .Select(f => new Models.CompanyFollower
                {
                    CandidateProfileId = f.CandidateProfileId!.Value,
                    UserId = f.CandidateProfile!.UserId,
                    Email = f.CandidateProfile.Email,
                    FirstName = f.CandidateProfile.FirstName,
                    IsTestAccount = f.CandidateProfile.IsTestAccount,
                    PremiumGrantedUntil = f.CandidateProfile.PremiumGrantedUntil,
                    PremiumPaidUntil = f.CandidateProfile.PremiumPaidUntil,
                })
                .ToListAsync();

        private static readonly System.Linq.Expressions.Expression<Func<RecruiterProfile, Models.Company>> CompanyProjection = r => new Models.Company
        {
            Id = r.Id,
            UserId = r.UserId,
            CompanyName = r.CompanyName,
            LogoPath = r.LogoPath,
            CompanySector = r.CompanySector,
            CompanySize = r.CompanySize,
            City = r.City,
            CompanyWebsite = r.CompanyWebsite,
            CompanyDescription = r.CompanyDescription,
            OpenOffersCount = r.Offers.Count(o => o.IsOpen == true && o.IsHidden != true),
            IsDisabled = r.IsDisabled == true,
            DisabledAt = r.DisabledAt,
            IsFounder = r.IsFounder == true,
            IsTestAccount = r.IsTestAccount,
            RecruiterName = ((r.FirstName ?? "") + " " + (r.LastName ?? "")).Trim(),
            RecruiterEmail = r.Email,
            OffersCount = r.Offers.Count,
            CreatedAt = r.CreatedAt,
        };

        public async Task<Models.Company?> SetCompanyDisabledAsync(int id, bool isDisabled)
        {
            var entity = await _context.RecruiterProfiles.FirstOrDefaultAsync(r => r.Id == id);
            if (entity == null) return null;

            entity.IsDisabled = isDisabled;
            entity.DisabledAt = isDisabled ? DateTime.UtcNow : null;
            await _context.SaveChangesAsync();
            return await _context.RecruiterProfiles.AsNoTracking().Where(r => r.Id == id).Select(CompanyProjection).FirstAsync();
        }

        // ---------- Page entreprise enrichie ----------

        public async Task<Models.CompanyPage> GetCompanyPageAsync(int recruiterProfileId)
        {
            var page = await _context.RecruiterProfiles.AsNoTracking()
                .Where(r => r.Id == recruiterProfileId)
                .Select(r => new Models.CompanyPage
                {
                    CoverPath = r.CoverPath,
                    Benefits = r.Benefits != null ? r.Benefits.ToList() : new List<string>(),
                    Culture = r.Culture,
                    LookingFor = r.LookingFor,
                    RecruitmentSteps = r.RecruitmentSteps != null ? r.RecruitmentSteps.ToList() : new List<string>(),
                    FoundedYear = r.FoundedYear,
                    LinkedinUrl = r.LinkedinUrl,
                    InstagramUrl = r.InstagramUrl,
                    FacebookUrl = r.FacebookUrl,
                    YoutubeUrl = r.YoutubeUrl,
                })
                .FirstOrDefaultAsync() ?? new Models.CompanyPage();

            var media = await _context.CompanyMedia.AsNoTracking()
                .Where(m => m.RecruiterProfileId == recruiterProfileId)
                .OrderBy(m => m.Position).ThenBy(m => m.Id)
                .ToListAsync();
            page.Photos = media.Where(m => m.Kind == Models.CompanyMediaKinds.Photo && m.Path != null)
                .Select(m => new Models.CompanyPhoto { Id = m.Id, Path = m.Path!, Caption = m.Title }).ToList();
            page.Videos = media.Where(m => m.Kind == Models.CompanyMediaKinds.Video && m.VideoId != null)
                .Select(m => new Models.CompanyVideo
                {
                    Id = m.Id, Provider = m.VideoProvider ?? Models.VideoProviders.Youtube, VideoId = m.VideoId!, ThumbnailUrl = m.ThumbnailUrl,
                    Title = m.Title ?? string.Empty, Quote = m.Quote,
                })
                .ToList();
            return page;
        }

        public Task UpdateCompanyPageAsync(int recruiterProfileId, Models.CompanyPage page) =>
            _context.RecruiterProfiles.Where(r => r.Id == recruiterProfileId).ExecuteUpdateAsync(s => s
                .SetProperty(r => r.Benefits, page.Benefits.ToArray())
                .SetProperty(r => r.Culture, page.Culture)
                .SetProperty(r => r.LookingFor, page.LookingFor)
                .SetProperty(r => r.RecruitmentSteps, page.RecruitmentSteps.ToArray())
                .SetProperty(r => r.FoundedYear, page.FoundedYear)
                .SetProperty(r => r.LinkedinUrl, page.LinkedinUrl)
                .SetProperty(r => r.InstagramUrl, page.InstagramUrl)
                .SetProperty(r => r.FacebookUrl, page.FacebookUrl)
                .SetProperty(r => r.YoutubeUrl, page.YoutubeUrl)
                .SetProperty(r => r.UpdatedAt, DateTime.UtcNow));

        public async Task<string?> SetCompanyCoverAsync(int recruiterProfileId, string? coverPath)
        {
            var entity = await _context.RecruiterProfiles.FirstAsync(r => r.Id == recruiterProfileId);
            var previous = entity.CoverPath;
            entity.CoverPath = coverPath;
            await _context.SaveChangesAsync();
            return previous;
        }

        public Task<int> CountCompanyMediaAsync(int recruiterProfileId, string kind) =>
            _context.CompanyMedia.CountAsync(m => m.RecruiterProfileId == recruiterProfileId && m.Kind == kind);

        public Task AddCompanyPhotoAsync(int recruiterProfileId, string path, string? caption) =>
            AddCompanyMediaAsync(new CompanyMedia { RecruiterProfileId = recruiterProfileId, Kind = Models.CompanyMediaKinds.Photo, Path = path, Title = caption });

        public Task AddCompanyVideoAsync(int recruiterProfileId, Models.CompanyVideo video) =>
            AddCompanyMediaAsync(new CompanyMedia
            {
                RecruiterProfileId = recruiterProfileId, Kind = Models.CompanyMediaKinds.Video,
                VideoProvider = video.Provider, VideoId = video.VideoId, ThumbnailUrl = video.ThumbnailUrl, Title = video.Title, Quote = video.Quote,
            });

        // Ajoutée à la fin de sa liste (photos ou vidéos)
        private async Task AddCompanyMediaAsync(CompanyMedia media)
        {
            media.Position = (await _context.CompanyMedia
                .Where(m => m.RecruiterProfileId == media.RecruiterProfileId && m.Kind == media.Kind)
                .MaxAsync(m => (int?)m.Position) ?? 0) + 1;
            media.CreatedAt = DateTime.UtcNow;
            _context.CompanyMedia.Add(media);
            await _context.SaveChangesAsync();
        }

        public async Task<(bool Deleted, string? Path)> DeleteCompanyMediaAsync(int recruiterProfileId, int mediaId)
        {
            var media = await _context.CompanyMedia.FirstOrDefaultAsync(m => m.Id == mediaId && m.RecruiterProfileId == recruiterProfileId);
            if (media == null) return (false, null);
            _context.CompanyMedia.Remove(media);
            await _context.SaveChangesAsync();
            // Photo : son fichier ; vidéo : sa miniature copiée chez nous
            return (true, media.Path ?? LocalFile(media.ThumbnailUrl));
        }

        private static string? LocalFile(string? path) => path != null && path.StartsWith("/uploads/") ? path : null;

        public async Task<List<string>> GetCompanyFilePathsAsync(int recruiterProfileId)
        {
            var media = await _context.CompanyMedia
                .Where(m => m.RecruiterProfileId == recruiterProfileId)
                .Select(m => new { m.Path, m.ThumbnailUrl })
                .ToListAsync();
            var paths = media.Select(m => m.Path ?? LocalFile(m.ThumbnailUrl)).OfType<string>().ToList();
            var cover = await _context.RecruiterProfiles.Where(r => r.Id == recruiterProfileId).Select(r => r.CoverPath).FirstOrDefaultAsync();
            if (cover != null) paths.Add(cover);
            return paths;
        }

        public async Task<Models.RecruiterProfile?> GetRecruiterProfileByIdAsync(int id)
        {
            return await _context.RecruiterProfiles.AsNoTracking().Where(r => r.Id == id).Select(Projection()).FirstOrDefaultAsync();
        }

        public async Task<Models.RecruiterProfile?> GetRecruiterProfileByUserIdAsync(string userId)
        {
            return await _context.RecruiterProfiles.AsNoTracking().Where(r => r.UserId == userId).Select(Projection()).FirstOrDefaultAsync();
        }

        public async Task<Models.RecruiterProfile> AddRecruiterProfileAsync(Models.RecruiterProfile model)
        {
            var entity = new RecruiterProfile { UserId = model.UserId, CreatedAt = DateTime.UtcNow };
            Apply(entity, model);
            _context.RecruiterProfiles.Add(entity);
            await _context.SaveChangesAsync();
            model.Id = entity.Id;
            model.CreatedAt = entity.CreatedAt;
            return model;
        }

        public async Task<Models.RecruiterProfile?> UpdateRecruiterProfileAsync(Models.RecruiterProfile model)
        {
            var entity = await _context.RecruiterProfiles.FirstOrDefaultAsync(r => r.Id == model.Id);
            if (entity == null) return null;

            Apply(entity, model);
            entity.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return await GetRecruiterProfileByIdAsync(entity.Id);
        }

        public Task SetTestAccountAsync(string userId, bool isTestAccount) =>
            _context.RecruiterProfiles.Where(p => p.UserId == userId && p.IsTestAccount != isTestAccount)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.IsTestAccount, isTestAccount));

        public async Task SetTestAccountsAsync(IReadOnlyCollection<string> testUserIds)
        {
            await _context.RecruiterProfiles.Where(p => p.IsTestAccount && !testUserIds.Contains(p.UserId!))
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.IsTestAccount, false));
            await _context.RecruiterProfiles.Where(p => !p.IsTestAccount && testUserIds.Contains(p.UserId!))
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.IsTestAccount, true));
        }

        public async Task<bool> DeleteRecruiterProfileAsync(int id)
        {
            var entity = await _context.RecruiterProfiles.FirstOrDefaultAsync(r => r.Id == id);
            if (entity == null) return false;

            _context.RecruiterProfiles.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        private static void Apply(RecruiterProfile entity, Models.RecruiterProfile model)
        {
            entity.FirstName = model.FirstName;
            entity.LastName = model.LastName;
            entity.Email = model.Email;
            entity.Phone = model.Phone;
            entity.IsPhoneVisible = model.IsPhoneVisible;
            entity.JobTitle = model.JobTitle;
            entity.CompanyName = model.CompanyName;
            entity.CompanySector = model.CompanySector;
            entity.CompanySize = model.CompanySize;
            entity.CompanyWebsite = model.CompanyWebsite;
            entity.CompanyDescription = model.CompanyDescription;
            entity.City = model.City;
            entity.LogoPath = model.LogoPath;
        }

        private static System.Linq.Expressions.Expression<Func<RecruiterProfile, Models.RecruiterProfile>> Projection() => r => new Models.RecruiterProfile
        {
            Id = r.Id,
            UserId = r.UserId,
            FirstName = r.FirstName,
            LastName = r.LastName,
            Email = r.Email,
            Phone = r.Phone,
            IsPhoneVisible = r.IsPhoneVisible == true,
            JobTitle = r.JobTitle,
            CompanyName = r.CompanyName,
            CompanySector = r.CompanySector,
            CompanySize = r.CompanySize,
            CompanyWebsite = r.CompanyWebsite,
            CompanyDescription = r.CompanyDescription,
            City = r.City,
            LogoPath = r.LogoPath,
            OffersCount = r.Offers.Count,
            OpenOffersCount = r.Offers.Count(o => o.IsOpen == true),
            IsDisabled = r.IsDisabled == true,
            DisabledAt = r.DisabledAt,
            IsTestAccount = r.IsTestAccount,
            // Recruteur Pro : modifié uniquement par l'administration et Stripe (jamais par le formulaire du profil)
            ProGrantedUntil = r.ProGrantedUntil,
            ProPaidUntil = r.ProPaidUntil,
            IsFounder = r.IsFounder == true,
            StripeCustomerId = r.StripeCustomerId,
            StripeSubscriptionId = r.StripeSubscriptionId,
            ProCancelAtPeriodEnd = r.ProCancelAtPeriodEnd == true,
            LastFreeSponsorAt = r.LastFreeSponsorAt,
            MatchAlertsEnabled = r.MatchAlertsDisabled != true,
            MessageAlertsEnabled = r.MessageAlertsDisabled != true,
            ApplicationAlertsEnabled = r.ApplicationAlertsDisabled != true,
            Siret = r.Siret,
            Siren = r.Siren,
            Rna = r.Rna,
            LegalName = r.LegalName,
            LegalAliases = r.LegalAliases,
            IdentityVerifiedAt = r.IdentityVerifiedAt,
            IsEmailConfirmed = r.IsEmailConfirmed == true,
            SignupIpHash = r.SignupIpHash,
            FounderSince = r.FounderSince,
            FounderRevokedAt = r.FounderRevokedAt,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt,
        };
    }
}
