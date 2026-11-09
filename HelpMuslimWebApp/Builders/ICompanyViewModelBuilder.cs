using System.Security.Claims;
using HelpMuslimWebApp.Api.Request;
using HelpMuslimWebApp.Api.ViewModels;

namespace HelpMuslimWebApp.Api.Builders
{
    public interface ICompanyViewModelBuilder
    {
        Task<IEnumerable<CompanyViewModel>> GetCompaniesAsync(string? search, string? sector, int? take, ClaimsPrincipal user);
        Task<IEnumerable<CompanySectorViewModel>> GetCompanySectorsAsync();
        Task<CompanyDetailViewModel?> GetCompanyAsync(int id, ClaimsPrincipal user);

        // Recruteur : sa page entreprise enrichie (« Mon entreprise › Ma page entreprise »)
        Task<CompanyPageViewModel> GetMyCompanyPageAsync(ClaimsPrincipal user);
        Task<CompanyPageViewModel> UpdateMyCompanyPageAsync(ClaimsPrincipal user, CompanyPageRequest model);
        Task<CompanyPageViewModel> SetMyCoverAsync(ClaimsPrincipal user, IFormFile? file);     // null = couverture retirée
        Task<CompanyPageViewModel> AddMyPhotoAsync(ClaimsPrincipal user, IFormFile file, string? caption);
        Task<CompanyPageViewModel> AddMyVideoAsync(ClaimsPrincipal user, CompanyVideoRequest model);
        Task<CompanyPageViewModel?> DeleteMyMediaAsync(ClaimsPrincipal user, int mediaId);     // null = introuvable

        // Candidat : entreprises suivies (alerte + email à chaque nouvelle offre)
        Task<IEnumerable<CompanyViewModel>> GetFollowedCompaniesAsync(ClaimsPrincipal user);
        Task<bool> FollowCompanyAsync(ClaimsPrincipal user, int companyId);     // false = entreprise introuvable
        Task UnfollowCompanyAsync(ClaimsPrincipal user, int companyId);

        // Administration : toutes les entreprises (désactivées comprises), désactivation / réactivation
        Task<IEnumerable<CompanyAdminViewModel>> GetAdminCompaniesAsync(string? search);
        Task<CompanyAdminViewModel?> SetCompanyDisabledAsync(int id, bool isDisabled);     // null = entreprise introuvable
    }
}
