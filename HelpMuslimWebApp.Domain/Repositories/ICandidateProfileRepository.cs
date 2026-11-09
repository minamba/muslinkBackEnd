using HelpMuslimWebApp.Domain.Models;

namespace HelpMuslimWebApp.Domain.Repositories
{
    public interface ICandidateProfileRepository
    {
        Task<IEnumerable<CandidateSummary>> GetCandidateProfilesAsync(string? search = null);
        Task<IEnumerable<CandidateSummary>> GetCandidatesForMatchingAsync();
        Task<CandidateProfile?> GetCandidateProfileByIdAsync(int id);
        Task<CandidateProfile?> GetCandidateProfileByUserIdAsync(string userId);
        Task<CandidateProfile?> GetCandidateProfileByShareTokenAsync(string token);
        Task<bool> UpdateCandidateShareAsync(int id, string? token, bool shareContact);
        Task<bool> SetCandidateHiddenAsync(int id, bool isHidden);
        // Comptes de test (IdentityServer) : un compte (jeton à la connexion), ou la liste complète (administration)
        Task SetTestAccountAsync(string userId, bool isTestAccount);
        Task SetTestAccountsAsync(IReadOnlyCollection<string> testUserIds);
        Task<bool> HasRelationWithRecruiterAsync(int candidateProfileId, int recruiterProfileId);
        Task<CandidateProfile> AddCandidateProfileAsync(CandidateProfile model);
        Task<CandidateProfile?> UpdateCandidateProfileAsync(CandidateProfile model);
        Task<bool> DeleteCandidateProfileAsync(int id);
    }
}
