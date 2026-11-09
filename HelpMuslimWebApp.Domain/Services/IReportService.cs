using HelpMuslimWebApp.Domain.Models;

namespace HelpMuslimWebApp.Domain.Services
{
    public interface IReportService
    {
        Task<Report> AddReportAsync(Report report);
        Task<Report?> GetReportAsync(int id);
        Task<List<Report>> GetReportsAsync(string? targetType);
        Task<Dictionary<string, int>> CountNewByTypeAsync();
        Task<bool> HasOpenReportAsync(string authorUserId, string targetType, int targetId);
        Task<int> CountReportsSinceAsync(string authorUserId, DateTime sinceUtc);
        Task<Report?> SetStatusAsync(int id, string status, string? adminNote, string? handledBy);
        Task<Report?> UpdateReportAsync(int id, string reason, string justification, string? adminNote);
        Task<bool> DeleteReportAsync(int id);
    }
}
