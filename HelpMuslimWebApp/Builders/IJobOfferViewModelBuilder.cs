using System.Security.Claims;
using HelpMuslimWebApp.Api.Request;
using HelpMuslimWebApp.Api.ViewModels;

namespace HelpMuslimWebApp.Api.Builders
{
    public interface IJobOfferViewModelBuilder
    {
        Task<PagedViewModel<JobOfferViewModel>> GetJobOffersAsync(OfferQueryRequest query, ClaimsPrincipal user);
        Task<PagedViewModel<JobOfferViewModel>> GetAllJobOffersAsync(OfferQueryRequest query);
        Task<WishOffersViewModel> GetWishOffersAsync(ClaimsPrincipal user, int? take);
        Task<IEnumerable<JobOfferViewModel>> GetMyJobOffersAsync(ClaimsPrincipal user);
        Task<JobOfferViewModel?> GetJobOfferByIdAsync(int id, ClaimsPrincipal user);
        Task<JobOfferViewModel> AddJobOfferAsync(ClaimsPrincipal user, JobOfferRequest model);
        Task<JobOfferViewModel?> UpdateJobOfferAsync(ClaimsPrincipal user, JobOfferRequest model);
        Task<JobOfferViewModel?> SetJobOfferStatusAsync(ClaimsPrincipal user, int id, bool isOpen, bool isFilled, int? hiredCandidateProfileId = null);
        Task<FilledOffersViewModel> GetFilledOffersAsync(int take);
        Task<JobOfferViewModel?> SetJobOfferVisibilityAsync(ClaimsPrincipal user, int id, bool isHidden);
        Task<int> DeleteOfferFillsAsync(int recruiterProfileId);

        // Candidat : offres favorites
        Task<IEnumerable<JobOfferViewModel>> GetFavoriteOffersAsync(ClaimsPrincipal user);
        Task<bool> AddFavoriteOfferAsync(ClaimsPrincipal user, int offerId);     // false = offre introuvable
        Task<bool> RemoveFavoriteOfferAsync(ClaimsPrincipal user, int offerId);
        Task<bool> DeleteJobOfferAsync(ClaimsPrincipal user, int id);
    }
}
