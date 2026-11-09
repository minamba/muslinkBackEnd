using System.Security.Claims;
using HelpMuslimWebApp.Domain.Models;

namespace HelpMuslimWebApp.Api.Services
{
    // Profil métier de l'utilisateur connecté, créé à la volée depuis les claims du token
    public interface IProfileResolver
    {
        Task<CandidateProfile> GetOrCreateCandidateAsync(ClaimsPrincipal user);
        Task<RecruiterProfile> GetOrCreateRecruiterAsync(ClaimsPrincipal user);
        Task<CandidateProfile?> FindCandidateAsync(ClaimsPrincipal user);
        Task<RecruiterProfile?> FindRecruiterAsync(ClaimsPrincipal user);

        // Recopie prénom, nom, email et téléphone du jeton dans le profil (après modification de « Mon compte »)
        Task SyncFromClaimsAsync(ClaimsPrincipal user);
    }
}
