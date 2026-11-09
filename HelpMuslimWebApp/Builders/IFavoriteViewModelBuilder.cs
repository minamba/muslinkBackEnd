using System.Security.Claims;
using HelpMuslimWebApp.Api.ViewModels;

namespace HelpMuslimWebApp.Api.Builders
{
    public interface IFavoriteViewModelBuilder
    {
        Task<IEnumerable<FavoriteViewModel>> GetMyFavoritesAsync(ClaimsPrincipal user);
        Task<bool> AddFavoriteAsync(ClaimsPrincipal user, int candidateProfileId);     // false = candidat introuvable
        Task<bool> RemoveFavoriteAsync(ClaimsPrincipal user, int candidateProfileId);
    }
}
