using HelpMuslimWebApp.Domain.Models;

namespace HelpMuslimWebApp.Domain.Services
{
    public interface IRecruiterProfileService
    {
        Task<IEnumerable<RecruiterProfile>> GetRecruiterProfilesAsync(string? search = null);
        Task<RecruiterProfile?> GetRecruiterProfileByIdAsync(int id);
        Task<RecruiterProfile?> GetRecruiterProfileByUserIdAsync(string userId);
        Task<RecruiterProfile> AddRecruiterProfileAsync(RecruiterProfile model);
        Task<RecruiterProfile?> UpdateRecruiterProfileAsync(RecruiterProfile model);
        Task<bool> DeleteRecruiterProfileAsync(int id);
        // Comptes de test (IdentityServer) : un compte (jeton à la connexion), ou la liste complète (administration)
        Task SetTestAccountAsync(string userId, bool isTestAccount);
        Task SetTestAccountsAsync(IReadOnlyCollection<string> testUserIds);

        // Annuaire des entreprises (profils recruteurs ayant renseigné leur entreprise)
        Task<IEnumerable<Company>> GetCompaniesAsync(string? search, string? sector, int take, bool includeTestAccounts);
        Task<IEnumerable<CompanySectorCount>> GetCompanySectorsAsync();     // activités présentes dans l'annuaire
        Task<Company?> GetCompanyAsync(int id);

        // Administration : toutes les entreprises (y compris désactivées) et désactivation
        Task<IEnumerable<Company>> GetAllCompaniesAsync(string? search);
        Task<Company?> SetCompanyDisabledAsync(int id, bool isDisabled);

        // Page entreprise enrichie (couverture, avantages, photos, vidéos…)
        Task<CompanyPage> GetCompanyPageAsync(int recruiterProfileId);
        // Textes, listes et liens (ni la couverture, ni les photos et vidéos)
        Task UpdateCompanyPageAsync(int recruiterProfileId, CompanyPage page);
        // Nouvelle couverture (null : retirée) ; retourne l'ancienne, à supprimer du disque
        Task<string?> SetCompanyCoverAsync(int recruiterProfileId, string? coverPath);
        Task<int> CountCompanyMediaAsync(int recruiterProfileId, string kind);
        Task AddCompanyPhotoAsync(int recruiterProfileId, string path, string? caption);
        Task AddCompanyVideoAsync(int recruiterProfileId, CompanyVideo video);
        // Photo ou vidéo de cette entreprise ; Path : fichier de la photo à supprimer du disque
        Task<(bool Deleted, string? Path)> DeleteCompanyMediaAsync(int recruiterProfileId, int mediaId);
        // Couverture et photos : fichiers à supprimer avec le profil
        Task<List<string>> GetCompanyFilePathsAsync(int recruiterProfileId);

        // Entreprises suivies par un candidat : alertes de nouvelle offre
        Task<IEnumerable<Company>> GetFollowedCompaniesAsync(int candidateProfileId, bool includeTestAccounts);
        Task FollowCompanyAsync(int candidateProfileId, int recruiterProfileId);
        Task<bool> UnfollowCompanyAsync(int candidateProfileId, int recruiterProfileId);
        Task<IEnumerable<CompanyFollower>> GetCompanyFollowersAsync(int recruiterProfileId);
    }
}
