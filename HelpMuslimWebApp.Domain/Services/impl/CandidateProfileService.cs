using System.Security.Cryptography;
using HelpMuslimWebApp.Domain.Models;
using HelpMuslimWebApp.Domain.Repositories;

namespace HelpMuslimWebApp.Domain.Services.impl
{
    public class CandidateProfileService : ICandidateProfileService
    {
        private readonly ICandidateProfileRepository _repository;

        public CandidateProfileService(ICandidateProfileRepository repository)
        {
            _repository = repository;
        }

        public Task<IEnumerable<CandidateSummary>> GetCandidateProfilesAsync(string? search = null) => _repository.GetCandidateProfilesAsync(search);
        public Task<IEnumerable<CandidateSummary>> GetCandidatesForMatchingAsync() => _repository.GetCandidatesForMatchingAsync();
        public Task<CandidateProfile?> GetCandidateProfileByIdAsync(int id) => _repository.GetCandidateProfileByIdAsync(id);
        public Task<CandidateProfile?> GetCandidateProfileByUserIdAsync(string userId) => _repository.GetCandidateProfileByUserIdAsync(userId);
        public Task<CandidateProfile?> GetSharedCandidateProfileAsync(string token) =>
            string.IsNullOrWhiteSpace(token) || token.Length > 32
                ? Task.FromResult<CandidateProfile?>(null)
                : _repository.GetCandidateProfileByShareTokenAsync(token);

        public async Task<string> EnableShareAsync(CandidateProfile profile, bool shareContact, bool renew)
        {
            // Jeton de 128 bits : le lien ne peut pas être deviné. Renouveler invalide l'ancien QR code.
            var token = renew || string.IsNullOrEmpty(profile.ShareToken)
                ? Convert.ToBase64String(RandomNumberGenerator.GetBytes(16)).TrimEnd('=').Replace('+', '-').Replace('/', '_')
                : profile.ShareToken;

            await _repository.UpdateCandidateShareAsync(profile.Id, token, shareContact);
            return token;
        }

        public Task<bool> DisableShareAsync(int id) => _repository.UpdateCandidateShareAsync(id, null, false);
        public Task<bool> SetCandidateHiddenAsync(int id, bool isHidden) => _repository.SetCandidateHiddenAsync(id, isHidden);
        public Task SetTestAccountAsync(string userId, bool isTestAccount) => _repository.SetTestAccountAsync(userId, isTestAccount);
        public Task SetTestAccountsAsync(IReadOnlyCollection<string> testUserIds) => _repository.SetTestAccountsAsync(testUserIds);
        public Task<bool> HasRelationWithRecruiterAsync(int candidateProfileId, int recruiterProfileId) =>
            _repository.HasRelationWithRecruiterAsync(candidateProfileId, recruiterProfileId);

        public Task<CandidateProfile> AddCandidateProfileAsync(CandidateProfile model) => _repository.AddCandidateProfileAsync(model);
        public Task<CandidateProfile?> UpdateCandidateProfileAsync(CandidateProfile model) => _repository.UpdateCandidateProfileAsync(model);
        public Task<bool> DeleteCandidateProfileAsync(int id) => _repository.DeleteCandidateProfileAsync(id);
    }
}
