using System.Security.Claims;
using HelpMuslimWebApp.Api.Request;
using HelpMuslimWebApp.Api.ViewModels;

namespace HelpMuslimWebApp.Api.Builders
{
    public interface IRecruiterProfileViewModelBuilder
    {
        Task<IEnumerable<RecruiterProfileViewModel>> GetRecruiterProfilesAsync(string? search);
        Task<RecruiterProfileViewModel?> GetRecruiterProfileByIdAsync(int id, ClaimsPrincipal viewer);
        Task<RecruiterProfileViewModel> GetMyProfileAsync(ClaimsPrincipal user);
        // Alerte quotidienne des nouveaux candidats à 70 % : depuis le compte, ou par le lien de l'email (sans connexion)
        Task<RecruiterProfileViewModel> SetMyMatchAlertsAsync(ClaimsPrincipal user, bool enabled);
        Task<bool> SetMatchAlertsByTokenAsync(string token, bool enabled);
        // Email « un candidat vous a écrit »
        Task<RecruiterProfileViewModel> SetMyMessageAlertsAsync(ClaimsPrincipal user, bool enabled);
        Task<bool> SetMessageAlertsByTokenAsync(string token, bool enabled);
        // Email « nouvelle candidature »
        Task<RecruiterProfileViewModel> SetMyApplicationAlertsAsync(ClaimsPrincipal user, bool enabled);
        Task<bool> SetApplicationAlertsByTokenAsync(string token, bool enabled);
        Task<RecruiterProfileViewModel?> UpdateMyProfileAsync(ClaimsPrincipal user, RecruiterProfileRequest model);
        Task<RecruiterProfileViewModel?> UpdateRecruiterProfileAsync(int id, AdminRecruiterProfileRequest model);   // compte de connexion mis à jour aussi
        Task<RecruiterProfileViewModel?> AddRecruiterProfileAsync(ProfileCreateRequest model);  // null = profil déjà existant
        Task<bool> DeleteRecruiterProfileAsync(int id);

        Task<RecruiterProfileViewModel> UpdateMyLogoAsync(ClaimsPrincipal user, IFormFile file);
        Task<RecruiterProfileViewModel> DeleteMyLogoAsync(ClaimsPrincipal user);
    }
}
