using HelpMuslimWebApp.Domain.Models;

namespace HelpMuslimWebApp.Domain.Services
{
    public interface IFavoriteService
    {
        Task<IEnumerable<Favorite>> GetFavoritesAsync(int recruiterProfileId);
        Task<bool> FavoriteExistsAsync(int recruiterProfileId, int candidateProfileId);
        Task<Favorite> AddFavoriteAsync(Favorite model);
        Task<bool> DeleteFavoriteAsync(int recruiterProfileId, int candidateProfileId);
    }
}
