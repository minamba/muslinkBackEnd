using HelpMuslimWebApp.Domain.Models;
using HelpMuslimWebApp.Domain.Repositories;

namespace HelpMuslimWebApp.Domain.Services.impl
{
    public class FounderService : IFounderService
    {
        private readonly IFounderRepository _repository;

        public FounderService(IFounderRepository repository)
        {
            _repository = repository;
        }

        public Task<int?> FindRecruiterBySirenAsync(string siren, int exceptRecruiterProfileId) => _repository.FindRecruiterBySirenAsync(siren, exceptRecruiterProfileId);
        public Task<int?> FindRecruiterByRnaAsync(string rna, int exceptRecruiterProfileId) => _repository.FindRecruiterByRnaAsync(rna, exceptRecruiterProfileId);
        public Task SetIdentityAsync(int recruiterProfileId, CompanyIdentity identity) => _repository.SetIdentityAsync(recruiterProfileId, identity);
        public Task<bool> SetIdentityVerifiedAsync(int recruiterProfileId, DateTime? verifiedAt) => _repository.SetIdentityVerifiedAsync(recruiterProfileId, verifiedAt);
        public Task SetEmailConfirmedAsync(int recruiterProfileId, bool confirmed) => _repository.SetEmailConfirmedAsync(recruiterProfileId, confirmed);
        public Task SetSignupIpHashAsync(int recruiterProfileId, string hash) => _repository.SetSignupIpHashAsync(recruiterProfileId, hash);
        public Task SetRevokedAsync(int recruiterProfileId, DateTime? revokedAt) => _repository.SetRevokedAsync(recruiterProfileId, revokedAt);
        public Task<int> CountFoundersAsync() => _repository.CountFoundersAsync();
        public Task<bool> HasPublishedOfferAsync(int recruiterProfileId) => _repository.HasPublishedOfferAsync(recruiterProfileId);
        public Task<List<FounderReview>> GetReviewAsync() => _repository.GetReviewAsync();

        public Task UpdateLaunchAsync(bool enabled, int slots, bool autoGrant)
        {
            if (slots < 0 || slots > Founders.MaxSlots) throw new ArgumentException($"Le nombre de places doit être compris entre 0 et {Founders.MaxSlots}.");
            return _repository.UpdateLaunchAsync(enabled, slots, autoGrant);
        }
    }
}
