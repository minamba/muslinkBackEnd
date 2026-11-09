using HelpMuslimWebApp.Domain.Models;

namespace HelpMuslimWebApp.Domain.Services
{
    public interface IMatchAlertService
    {
        Task<HashSet<(int JobOfferId, int CandidateProfileId)>> GetAlertedPairsAsync();
        Task<HashSet<(int JobOfferId, int CandidateProfileId)>> GetApplicationPairsAsync();
        Task AddAlertsAsync(IReadOnlyCollection<MatchAlertPair> pairs);
        Task<string> GetOrCreateTokenAsync(int recruiterProfileId);
        Task<bool> SetEnabledAsync(int recruiterProfileId, bool enabled);
        Task<bool> SetEnabledByTokenAsync(string token, bool enabled);
        // Email « un candidat vous a écrit »
        Task<bool> SetMessageAlertsEnabledAsync(int recruiterProfileId, bool enabled);
        Task<bool> SetMessageAlertsEnabledByTokenAsync(string token, bool enabled);
        Task<bool> SetApplicationAlertsEnabledAsync(int recruiterProfileId, bool enabled);
        Task<bool> SetApplicationAlertsEnabledByTokenAsync(string token, bool enabled);
    }
}
