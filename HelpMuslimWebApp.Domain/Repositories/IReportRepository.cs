using HelpMuslimWebApp.Domain.Models;

namespace HelpMuslimWebApp.Domain.Repositories
{
    public interface IReportRepository
    {
        Task<Report> AddReportAsync(Report report);
        Task<Report?> GetReportAsync(int id);
        // Du plus récent au plus ancien ; type null : tous
        Task<List<Report>> GetReportsAsync(string? targetType);
        // Signalements « nouveau » par type d'élément (pastilles des onglets)
        Task<Dictionary<string, int>> CountNewByTypeAsync();
        // Signalement encore ouvert (nouveau ou en cours d'analyse) du même auteur sur le même élément
        Task<bool> HasOpenReportAsync(string authorUserId, string targetType, int targetId);
        Task<int> CountReportsSinceAsync(string authorUserId, DateTime sinceUtc);
        Task<Report?> SetStatusAsync(int id, string status, string? adminNote, string? handledBy);
        // Correction par l'administration (raison, justification, note d'annulation) ; null : introuvable
        Task<Report?> UpdateReportAsync(int id, string reason, string justification, string? adminNote);
        Task<bool> DeleteReportAsync(int id);
    }
}
