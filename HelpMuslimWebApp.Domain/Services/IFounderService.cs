using HelpMuslimWebApp.Domain.Models;

namespace HelpMuslimWebApp.Domain.Services
{
    public interface IFounderService
    {
        Task<int?> FindRecruiterBySirenAsync(string siren, int exceptRecruiterProfileId);
        Task<int?> FindRecruiterByRnaAsync(string rna, int exceptRecruiterProfileId);
        Task SetIdentityAsync(int recruiterProfileId, CompanyIdentity identity);
        Task<bool> SetIdentityVerifiedAsync(int recruiterProfileId, DateTime? verifiedAt);
        Task SetEmailConfirmedAsync(int recruiterProfileId, bool confirmed);
        Task SetSignupIpHashAsync(int recruiterProfileId, string hash);
        // Statut retiré par l'administration (null : de nouveau attribuable)
        Task SetRevokedAsync(int recruiterProfileId, DateTime? revokedAt);
        Task<int> CountFoundersAsync();
        Task<bool> HasPublishedOfferAsync(int recruiterProfileId);
        Task<List<FounderReview>> GetReviewAsync();
        Task UpdateLaunchAsync(bool enabled, int slots, bool autoGrant);
    }
}
