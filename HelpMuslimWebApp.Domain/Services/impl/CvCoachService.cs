using HelpMuslimWebApp.Domain.Models;
using HelpMuslimWebApp.Domain.Repositories;

namespace HelpMuslimWebApp.Domain.Services.impl
{
    public class CvCoachService : ICvCoachService
    {
        private readonly ICvCoachRepository _repository;

        public CvCoachService(ICvCoachRepository repository)
        {
            _repository = repository;
        }

        public Task<CvCoachAnalysis?> GetLatestAsync(int candidateProfileId, bool byAdmin) => _repository.GetLatestAsync(candidateProfileId, byAdmin);

        public Task<CvCoachAnalysis> AddAsync(CvCoachAnalysis model)
        {
            model.CreatedAt = DateTime.UtcNow;
            return _repository.AddAsync(model);
        }

        public Task<int> DeleteCandidateAnalysesAsync(int candidateProfileId) => _repository.DeleteCandidateAnalysesAsync(candidateProfileId);

        public Task<int> CountSinceAsync(int candidateProfileId, DateTime sinceUtc) => _repository.CountSinceAsync(candidateProfileId, sinceUtc);
    }
}
