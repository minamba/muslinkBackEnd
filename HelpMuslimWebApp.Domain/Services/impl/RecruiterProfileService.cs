using HelpMuslimWebApp.Domain.Models;
using HelpMuslimWebApp.Domain.Repositories;

namespace HelpMuslimWebApp.Domain.Services.impl
{
    public class RecruiterProfileService : IRecruiterProfileService
    {
        private readonly IRecruiterProfileRepository _repository;

        public RecruiterProfileService(IRecruiterProfileRepository repository)
        {
            _repository = repository;
        }

        public Task<IEnumerable<RecruiterProfile>> GetRecruiterProfilesAsync(string? search = null) => _repository.GetRecruiterProfilesAsync(search);
        public Task<RecruiterProfile?> GetRecruiterProfileByIdAsync(int id) => _repository.GetRecruiterProfileByIdAsync(id);
        public Task<RecruiterProfile?> GetRecruiterProfileByUserIdAsync(string userId) => _repository.GetRecruiterProfileByUserIdAsync(userId);
        public Task<RecruiterProfile> AddRecruiterProfileAsync(RecruiterProfile model) => _repository.AddRecruiterProfileAsync(model);
        public Task<RecruiterProfile?> UpdateRecruiterProfileAsync(RecruiterProfile model) => _repository.UpdateRecruiterProfileAsync(model);
        public Task<bool> DeleteRecruiterProfileAsync(int id) => _repository.DeleteRecruiterProfileAsync(id);
        public Task SetTestAccountAsync(string userId, bool isTestAccount) => _repository.SetTestAccountAsync(userId, isTestAccount);
        public Task SetTestAccountsAsync(IReadOnlyCollection<string> testUserIds) => _repository.SetTestAccountsAsync(testUserIds);

        public Task<IEnumerable<Company>> GetCompaniesAsync(string? search, string? sector, int take, bool includeTestAccounts) =>
            _repository.GetCompaniesAsync(search, sector, Math.Clamp(take, 1, 100), includeTestAccounts: includeTestAccounts);
        public Task<IEnumerable<CompanySectorCount>> GetCompanySectorsAsync() => _repository.GetCompanySectorsAsync();
        public Task<Company?> GetCompanyAsync(int id) => _repository.GetCompanyAsync(id);
        public Task<IEnumerable<Company>> GetAllCompaniesAsync(string? search) => _repository.GetCompaniesAsync(search, null, 1000, includeDisabled: true, includeTestAccounts: true);
        public Task<Company?> SetCompanyDisabledAsync(int id, bool isDisabled) => _repository.SetCompanyDisabledAsync(id, isDisabled);
        public Task<CompanyPage> GetCompanyPageAsync(int recruiterProfileId) => _repository.GetCompanyPageAsync(recruiterProfileId);
        public Task UpdateCompanyPageAsync(int recruiterProfileId, CompanyPage page) => _repository.UpdateCompanyPageAsync(recruiterProfileId, page);
        public Task<string?> SetCompanyCoverAsync(int recruiterProfileId, string? coverPath) => _repository.SetCompanyCoverAsync(recruiterProfileId, coverPath);
        public Task<int> CountCompanyMediaAsync(int recruiterProfileId, string kind) => _repository.CountCompanyMediaAsync(recruiterProfileId, kind);
        public Task AddCompanyPhotoAsync(int recruiterProfileId, string path, string? caption) => _repository.AddCompanyPhotoAsync(recruiterProfileId, path, caption);
        public Task AddCompanyVideoAsync(int recruiterProfileId, CompanyVideo video) => _repository.AddCompanyVideoAsync(recruiterProfileId, video);
        public Task<(bool Deleted, string? Path)> DeleteCompanyMediaAsync(int recruiterProfileId, int mediaId) => _repository.DeleteCompanyMediaAsync(recruiterProfileId, mediaId);
        public Task<List<string>> GetCompanyFilePathsAsync(int recruiterProfileId) => _repository.GetCompanyFilePathsAsync(recruiterProfileId);
        public Task<IEnumerable<Company>> GetFollowedCompaniesAsync(int candidateProfileId, bool includeTestAccounts) => _repository.GetFollowedCompaniesAsync(candidateProfileId, includeTestAccounts);
        public Task FollowCompanyAsync(int candidateProfileId, int recruiterProfileId) => _repository.FollowCompanyAsync(candidateProfileId, recruiterProfileId);
        public Task<bool> UnfollowCompanyAsync(int candidateProfileId, int recruiterProfileId) => _repository.UnfollowCompanyAsync(candidateProfileId, recruiterProfileId);
        public Task<IEnumerable<CompanyFollower>> GetCompanyFollowersAsync(int recruiterProfileId) => _repository.GetCompanyFollowersAsync(recruiterProfileId);
    }
}
