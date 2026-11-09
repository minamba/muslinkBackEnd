using HelpMuslimWebApp.Domain.Models;

namespace HelpMuslimWebApp.Domain.Repositories
{
    public interface IJobApplicationRepository
    {
        Task<IEnumerable<JobApplication>> GetJobApplicationsAsync(int? jobOfferId = null, int? candidateProfileId = null);
        Task<IEnumerable<JobApplication>> GetCandidateApplicationsAsync(int candidateProfileId, DateTime closedSince);
        Task<bool> HideJobApplicationAsync(int id);
        Task<IEnumerable<string>> GetApplicationCvPathsAsync(int? jobOfferId = null, int? candidateProfileId = null, int? recruiterProfileId = null);
        Task<JobApplication?> GetJobApplicationByIdAsync(int id);
        Task<bool> JobApplicationExistsAsync(int jobOfferId, int candidateProfileId);
        Task<JobApplication> AddJobApplicationAsync(JobApplication model);
        Task<bool> DeleteJobApplicationAsync(int id);
    }
}
