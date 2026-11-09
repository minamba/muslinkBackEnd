using System.Security.Claims;
using HelpMuslimWebApp.Api.ViewModels;

namespace HelpMuslimWebApp.Api.Builders
{
    public interface ICvCoachViewModelBuilder
    {
        // État du coach et dernière analyse du candidat connecté
        Task<CvCoachViewModel> GetAsync(ClaimsPrincipal user);

        // Nouvelle analyse (Premium) au regard de « Ce que je cherche » : source = « cv » (CV et profil) ou « profile » (profil seul) ;
        // non précisée : le CV s'il est lisible, sinon le profil
        Task<CvCoachViewModel> AnalyzeAsync(ClaimsPrincipal user, string? source, CancellationToken cancellationToken);

        // Le candidat supprime son analyse (et les précédentes) : elles ne lui sont plus montrées et leur contenu est effacé,
        // mais elles comptent toujours dans le quota du mois
        Task<CvCoachViewModel> DeleteAnalysesAsync(ClaimsPrincipal user);

        // « Ajouter à mes compétences » depuis une suggestion du coach ; retourne les compétences du profil
        Task<List<string>> AddSkillAsync(ClaimsPrincipal user, string skill);

        // Super admin, depuis l'administration : coach d'un candidat, analyses illimitées et visibles de lui seul (null : candidat introuvable)
        Task<CvCoachViewModel?> GetForAdminAsync(int candidateProfileId);
        Task<CvCoachViewModel?> AnalyzeForAdminAsync(int candidateProfileId, string? source, CancellationToken cancellationToken);
    }
}
