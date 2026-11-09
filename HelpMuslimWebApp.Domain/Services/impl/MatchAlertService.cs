using HelpMuslimWebApp.Domain.Models;
using HelpMuslimWebApp.Domain.Repositories;

namespace HelpMuslimWebApp.Domain.Services.impl
{
    public class MatchAlertService : IMatchAlertService
    {
        private readonly IMatchAlertRepository _repository;

        public MatchAlertService(IMatchAlertRepository repository)
        {
            _repository = repository;
        }

        public Task<HashSet<(int JobOfferId, int CandidateProfileId)>> GetAlertedPairsAsync() => _repository.GetAlertedPairsAsync();
        public Task<HashSet<(int JobOfferId, int CandidateProfileId)>> GetApplicationPairsAsync() => _repository.GetApplicationPairsAsync();
        public Task AddAlertsAsync(IReadOnlyCollection<MatchAlertPair> pairs) => pairs.Count == 0 ? Task.CompletedTask : _repository.AddAlertsAsync(pairs);
        public Task<string> GetOrCreateTokenAsync(int recruiterProfileId) => _repository.GetOrCreateTokenAsync(recruiterProfileId);
        public Task<bool> SetEnabledAsync(int recruiterProfileId, bool enabled) => _repository.SetEnabledAsync(recruiterProfileId, enabled);

        public Task<bool> SetEnabledByTokenAsync(string token, bool enabled) =>
            string.IsNullOrWhiteSpace(token) || token.Length > 64 ? Task.FromResult(false) : _repository.SetEnabledByTokenAsync(token.Trim(), enabled);

        public Task<bool> SetMessageAlertsEnabledAsync(int recruiterProfileId, bool enabled) => _repository.SetMessageAlertsEnabledAsync(recruiterProfileId, enabled);

        public Task<bool> SetMessageAlertsEnabledByTokenAsync(string token, bool enabled) =>
            string.IsNullOrWhiteSpace(token) || token.Length > 64 ? Task.FromResult(false) : _repository.SetMessageAlertsEnabledByTokenAsync(token.Trim(), enabled);

        public Task<bool> SetApplicationAlertsEnabledAsync(int recruiterProfileId, bool enabled) => _repository.SetApplicationAlertsEnabledAsync(recruiterProfileId, enabled);

        public Task<bool> SetApplicationAlertsEnabledByTokenAsync(string token, bool enabled) =>
            string.IsNullOrWhiteSpace(token) || token.Length > 64 ? Task.FromResult(false) : _repository.SetApplicationAlertsEnabledByTokenAsync(token.Trim(), enabled);
    }
}
