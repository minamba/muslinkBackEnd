using HelpMuslimWebApp.Dal.Entities;
using HelpMuslimWebApp.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Models = HelpMuslimWebApp.Domain.Models;

namespace HelpMuslimWebApp.Dal.Repositories
{
    public class FavoriteRepository : IFavoriteRepository
    {
        private readonly HelpMuslimWebAppDatabaseContext _context;

        public FavoriteRepository(HelpMuslimWebAppDatabaseContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Models.Favorite>> GetFavoritesAsync(int recruiterProfileId)
        {
            return await _context.Favorites.AsNoTracking()
                .Where(f => f.RecruiterProfileId == recruiterProfileId)
                .OrderByDescending(f => f.CreatedAt)
                .Select(f => new Models.Favorite
                {
                    Id = f.Id,
                    RecruiterProfileId = f.RecruiterProfileId,
                    CandidateProfileId = f.CandidateProfileId,
                    CreatedAt = f.CreatedAt,
                    Candidate = new Models.CandidateSummary
                    {
                        Id = f.CandidateProfile!.Id,
                        UserId = f.CandidateProfile.UserId,
                        FirstName = f.CandidateProfile.FirstName,
                        LastName = f.CandidateProfile.LastName,
                        Email = f.CandidateProfile.Email,
                        Headline = f.CandidateProfile.Headline,
                        City = f.CandidateProfile.City,
                        Situation = f.CandidateProfile.Situation,
                        DesiredJob = f.CandidateProfile.DesiredJob,
                        PhotoPath = f.CandidateProfile.PhotoPath,
                        CreatedAt = f.CandidateProfile.CreatedAt,
                    },
                })
                .ToListAsync();
        }

        public Task<bool> FavoriteExistsAsync(int recruiterProfileId, int candidateProfileId)
        {
            return _context.Favorites.AnyAsync(f => f.RecruiterProfileId == recruiterProfileId && f.CandidateProfileId == candidateProfileId);
        }

        public async Task<Models.Favorite> AddFavoriteAsync(Models.Favorite model)
        {
            var entity = new Favorite
            {
                RecruiterProfileId = model.RecruiterProfileId,
                CandidateProfileId = model.CandidateProfileId,
                CreatedAt = model.CreatedAt ?? DateTime.UtcNow,
            };
            _context.Favorites.Add(entity);
            await _context.SaveChangesAsync();
            model.Id = entity.Id;
            return model;
        }

        public async Task<bool> DeleteFavoriteAsync(int recruiterProfileId, int candidateProfileId)
        {
            var entity = await _context.Favorites
                .FirstOrDefaultAsync(f => f.RecruiterProfileId == recruiterProfileId && f.CandidateProfileId == candidateProfileId);
            if (entity == null) return false;

            _context.Favorites.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
