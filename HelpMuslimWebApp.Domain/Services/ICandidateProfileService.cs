using HelpMuslimWebApp.Domain.Models;

namespace HelpMuslimWebApp.Domain.Services
{
    public interface ICandidateProfileService
    {
        Task<IEnumerable<CandidateSummary>> GetCandidateProfilesAsync(string? search = null);
        Task<IEnumerable<CandidateSummary>> GetCandidatesForMatchingAsync();
        Task<CandidateProfile?> GetCandidateProfileByIdAsync(int id);
        Task<CandidateProfile?> GetCandidateProfileByUserIdAsync(string userId);

        // Fiche partagée (QR code) : lien public secret, que le candidat peut renouveler ou désactiver
        Task<CandidateProfile?> GetSharedCandidateProfileAsync(string token);
        Task<string> EnableShareAsync(CandidateProfile profile, bool shareContact, bool renew);
        Task<bool> DisableShareAsync(int id);

        // Profil invisible : absent des suggestions ; visible seulement des recruteurs déjà en relation (candidature, messagerie)
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
