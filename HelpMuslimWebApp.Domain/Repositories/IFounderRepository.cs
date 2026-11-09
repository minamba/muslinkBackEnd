using HelpMuslimWebApp.Domain.Models;

namespace HelpMuslimWebApp.Domain.Repositories
{
    public interface IFounderRepository
    {
        // Autre compte recruteur déjà rattaché à cette entreprise (SIREN) ou association (RNA)
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
        // Fondatrices et entreprises qui ont renseigné un SIRET ou un RNA
        Task<List<FounderReview>> GetReviewAsync();
        Task UpdateLaunchAsync(bool enabled, int slots, bool autoGrant);
    }
}
