using HelpMuslimWebApp.Domain.Models;

namespace HelpMuslimWebApp.Domain.Services
{
    public interface ICvCoachService
    {
        Task<CvCoachAnalysis?> GetLatestAsync(int candidateProfileId, bool byAdmin);
        Task<CvCoachAnalysis> AddAsync(CvCoachAnalysis model);
        Task<int> DeleteCandidateAnalysesAsync(int candidateProfileId);
        Task<int> CountSinceAsync(int candidateProfileId, DateTime sinceUtc);
    }
}
