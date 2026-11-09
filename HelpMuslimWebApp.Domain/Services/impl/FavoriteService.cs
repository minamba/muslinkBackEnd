using HelpMuslimWebApp.Domain.Models;
using HelpMuslimWebApp.Domain.Repositories;

namespace HelpMuslimWebApp.Domain.Services.impl
{
    public class FavoriteService : IFavoriteService
    {
        private readonly IFavoriteRepository _repository;

        public FavoriteService(IFavoriteRepository repository)
        {
            _repository = repository;
        }

        public Task<IEnumerable<Favorite>> GetFavoritesAsync(int recruiterProfileId) => _repository.GetFavoritesAsync(recruiterProfileId);
        public Task<bool> FavoriteExistsAsync(int recruiterProfileId, int candidateProfileId) => _repository.FavoriteExistsAsync(recruiterProfileId, candidateProfileId);

        public Task<Favorite> AddFavoriteAsync(Favorite model)
        {
            model.CreatedAt = DateTime.UtcNow;
            return _repository.AddFavoriteAsync(model);
        }

        public Task<bool> DeleteFavoriteAsync(int recruiterProfileId, int candidateProfileId) => _repository.DeleteFavoriteAsync(recruiterProfileId, candidateProfileId);
    }
}
