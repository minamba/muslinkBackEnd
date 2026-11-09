using HelpMuslimWebApp.Domain.Models;
using HelpMuslimWebApp.Domain.Repositories;

namespace HelpMuslimWebApp.Domain.Services.impl
{
    public class JobOfferService : IJobOfferService
    {
        private readonly IJobOfferRepository _repository;

        public JobOfferService(IJobOfferRepository repository)
        {
            _repository = repository;
        }

        public Task<PagedResult<JobOffer>> GetJobOffersAsync(OfferFilter filter)
        {
            // Bornes de pagination
            filter.Page = Math.Max(1, filter.Page);
            filter.PageSize = Math.Clamp(filter.PageSize, 1, 50);
            return _repository.GetJobOffersAsync(filter);
        }

        public Task<IEnumerable<JobOffer>> GetFilteredJobOffersAsync(OfferFilter filter) => _repository.GetFilteredJobOffersAsync(filter);
        public Task IncrementViewsAsync(int jobOfferId) => _repository.IncrementViewsAsync(jobOfferId);
        public Task<JobOffer?> GetJobOfferByIdAsync(int id) => _repository.GetJobOfferByIdAsync(id);

        public Task<JobOffer> AddJobOfferAsync(JobOffer model)
        {
            model.IsOpen = true;
            model.CreatedAt = DateTime.UtcNow;
            model.ClosedAt = null;
            return _repository.AddJobOfferAsync(model);
        }

        public Task<JobOffer?> UpdateJobOfferAsync(JobOffer model) => _repository.UpdateJobOfferAsync(model);
        public Task<JobOffer?> SetJobOfferOpenAsync(int id, bool isOpen, bool isFilled = false) => _repository.SetJobOfferOpenAsync(id, isOpen, isFilled);
        public Task<int> MarkOffersInDiscussionAsync(int recruiterProfileId, int candidateProfileId) =>
            _repository.MarkOffersInDiscussionAsync(recruiterProfileId, candidateProfileId);
        public Task<int> DeleteOfferFillsAsync(int recruiterProfileId) => _repository.DeleteOfferFillsAsync(recruiterProfileId);
        public Task<JobOffer?> SetJobOfferHiddenAsync(int id, bool isHidden) => _repository.SetJobOfferHiddenAsync(id, isHidden);

        public Task<IEnumerable<JobOffer>> GetFavoriteOffersAsync(int candidateProfileId) => _repository.GetFavoriteOffersAsync(candidateProfileId);
        public Task<bool> OfferFavoriteExistsAsync(int candidateProfileId, int jobOfferId) => _repository.OfferFavoriteExistsAsync(candidateProfileId, jobOfferId);
        public Task AddOfferFavoriteAsync(int candidateProfileId, int jobOfferId) => _repository.AddOfferFavoriteAsync(candidateProfileId, jobOfferId);
        public Task<bool> DeleteOfferFavoriteAsync(int candidateProfileId, int jobOfferId) => _repository.DeleteOfferFavoriteAsync(candidateProfileId, jobOfferId);
        public Task<FilledOffersStats> GetFilledOffersStatsAsync(int take) => _repository.GetFilledOffersStatsAsync(Math.Clamp(take, 1, 20));
        public Task<bool> DeleteJobOfferAsync(int id) => _repository.DeleteJobOfferAsync(id);
    }
}
