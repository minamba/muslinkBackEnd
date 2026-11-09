using HelpMuslimWebApp.Domain.Models;
using HelpMuslimWebApp.Domain.Repositories;

namespace HelpMuslimWebApp.Domain.Services.impl
{
    public class JobApplicationService : IJobApplicationService
    {
        private readonly IJobApplicationRepository _repository;

        public JobApplicationService(IJobApplicationRepository repository)
        {
            _repository = repository;
        }

        public Task<IEnumerable<JobApplication>> GetJobApplicationsAsync(int? jobOfferId = null, int? candidateProfileId = null)
            => _repository.GetJobApplicationsAsync(jobOfferId, candidateProfileId);
        // Délai pendant lequel une candidature à une offre fermée reste dans « Mes candidatures »
        public static readonly TimeSpan ClosedOfferRetention = TimeSpan.FromDays(7);

        public Task<IEnumerable<JobApplication>> GetCandidateApplicationsAsync(int candidateProfileId)
            => _repository.GetCandidateApplicationsAsync(candidateProfileId, DateTime.UtcNow - ClosedOfferRetention);
        public Task<bool> HideJobApplicationAsync(int id) => _repository.HideJobApplicationAsync(id);
        public Task<IEnumerable<string>> GetApplicationCvPathsAsync(int? jobOfferId = null, int? candidateProfileId = null, int? recruiterProfileId = null)
            => _repository.GetApplicationCvPathsAsync(jobOfferId, candidateProfileId, recruiterProfileId);
        public Task<JobApplication?> GetJobApplicationByIdAsync(int id) => _repository.GetJobApplicationByIdAsync(id);
        public Task<bool> JobApplicationExistsAsync(int jobOfferId, int candidateProfileId) => _repository.JobApplicationExistsAsync(jobOfferId, candidateProfileId);

        public Task<JobApplication> AddJobApplicationAsync(JobApplication model)
        {
            model.CreatedAt = DateTime.UtcNow;
            return _repository.AddJobApplicationAsync(model);
        }

        public Task<bool> DeleteJobApplicationAsync(int id) => _repository.DeleteJobApplicationAsync(id);
    }
}
