using HelpMuslimWebApp.Domain.Models;

namespace HelpMuslimWebApp.Domain.Services
{
    public interface IJobApplicationService
    {
        Task<IEnumerable<JobApplication>> GetJobApplicationsAsync(int? jobOfferId = null, int? candidateProfileId = null);
        // « Mes candidatures » : une offre fermée disparaît au bout d'une semaine, ou plus tôt si le candidat la retire de sa liste
        Task<IEnumerable<JobApplication>> GetCandidateApplicationsAsync(int candidateProfileId);
        Task<bool> HideJobApplicationAsync(int id);

        // Copies de CV jointes aux candidatures (à supprimer du disque avec les candidatures)
        Task<IEnumerable<string>> GetApplicationCvPathsAsync(int? jobOfferId = null, int? candidateProfileId = null, int? recruiterProfileId = null);
        Task<JobApplication?> GetJobApplicationByIdAsync(int id);
        Task<bool> JobApplicationExistsAsync(int jobOfferId, int candidateProfileId);
        Task<JobApplication> AddJobApplicationAsync(JobApplication model);
        Task<bool> DeleteJobApplicationAsync(int id);
    }
}
