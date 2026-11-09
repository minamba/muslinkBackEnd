using HelpMuslimWebApp.Domain.Models;

namespace HelpMuslimWebApp.Domain.Repositories
{
    public interface IJobOfferRepository
    {
        Task<PagedResult<JobOffer>> GetJobOffersAsync(OfferFilter filter);
        Task<IEnumerable<JobOffer>> GetFilteredJobOffersAsync(OfferFilter filter);
        Task IncrementViewsAsync(int jobOfferId);
        Task<JobOffer?> GetJobOfferByIdAsync(int id);
        Task<JobOffer> AddJobOfferAsync(JobOffer model);
        Task<JobOffer?> UpdateJobOfferAsync(JobOffer model);
        Task<JobOffer?> SetJobOfferOpenAsync(int id, bool isOpen, bool isFilled);
        Task<JobOffer?> SetJobOfferHiddenAsync(int id, bool isHidden);
        Task<FilledOffersStats> GetFilledOffersStatsAsync(int take);
        Task<int> MarkOffersInDiscussionAsync(int recruiterProfileId, int candidateProfileId);
        Task<int> DeleteOfferFillsAsync(int recruiterProfileId);

        Task<IEnumerable<JobOffer>> GetFavoriteOffersAsync(int candidateProfileId);
        Task<bool> OfferFavoriteExistsAsync(int candidateProfileId, int jobOfferId);
        Task AddOfferFavoriteAsync(int candidateProfileId, int jobOfferId);
        Task<bool> DeleteOfferFavoriteAsync(int candidateProfileId, int jobOfferId);
        Task<bool> DeleteJobOfferAsync(int id);
    }
}
