using System.Security.Claims;
using HelpMuslimWebApp.Api.Request;
using HelpMuslimWebApp.Api.ViewModels;

namespace HelpMuslimWebApp.Api.Builders
{
    public interface IReportViewModelBuilder
    {
        // Candidat : offre, entreprise, recruteur. Recruteur : candidat, offre, entreprise ou recruteur autres que les siens.
        // Prévient l'équipe sur Telegram et envoie un accusé de réception au signalant.
        Task CreateAsync(ClaimsPrincipal user, ReportRequest request);

        Task<IEnumerable<ReportViewModel>> GetReportsAsync(string? targetType);
        // Signalements « nouveau » par type (tous les types présents, 0 par défaut)
        Task<Dictionary<string, int>> CountNewAsync();
        // Email au signalant à chaque changement de statut ; null : signalement introuvable
        Task<ReportViewModel?> SetStatusAsync(ClaimsPrincipal admin, int id, ReportStatusRequest request);
        // Correction de la raison, de la justification ou de la note d'annulation, sans email ; null : introuvable
        Task<ReportViewModel?> UpdateAsync(int id, ReportUpdateRequest request);
        Task<bool> DeleteAsync(int id);
    }
}
