using System.Security.Claims;
using HelpMuslimWebApp.Api.Request;
using HelpMuslimWebApp.Api.ViewModels;
using HelpMuslimWebApp.Domain.Models;

namespace HelpMuslimWebApp.Api.Builders
{
    public interface IFounderViewModelBuilder
    {
        // Offre de lancement : places restantes et, pour un recruteur, ses conditions (attribue la place si elles sont remplies)
        Task<LaunchViewModel> GetLaunchAsync(ClaimsPrincipal viewer);

        // Mon entreprise : nom de la structure et SIRET (ou RNA) vérifiés ensemble auprès de l'annuaire officiel ;
        // un seul compte par entreprise. null : identité inchangée
        // exempt : compte de test en mode test, SIRET / RNA enregistrés sans aucun contrôle
        Task<CompanyIdentity?> ResolveIdentityAsync(RecruiterProfile recruiter, string? companyName, string? siretInput, string? rnaInput, bool exempt = false);

        // Offre publiée, entreprise vérifiée : place de fondatrice si les conditions sont remplies (jamais d'erreur)
        Task TryGrantFounderAsync(ClaimsPrincipal user);

        // ---------- Administration ----------
        Task<FounderAdminViewModel> GetAdminAsync();
        Task<FounderAdminViewModel> UpdateLaunchAsync(LaunchSettingsRequest model);
        Task<FounderAdminViewModel> SetIdentityVerifiedAsync(int recruiterProfileId, bool verified);
        Task<FounderAdminViewModel> AppointAsync(int recruiterProfileId);
        Task<FounderAdminViewModel> RevokeAsync(int recruiterProfileId);
    }
}
