using HelpMuslimWebApp.Domain.Models;
using HelpMuslimWebApp.Domain.Repositories;

namespace HelpMuslimWebApp.Domain.Services.impl
{
    public class ReportService : IReportService
    {
        private readonly IReportRepository _repository;

        public ReportService(IReportRepository repository)
        {
            _repository = repository;
        }

        public Task<Report> AddReportAsync(Report report)
        {
            report.CreatedAt = DateTime.UtcNow;
            report.Status = ReportStatuses.New;
            return _repository.AddReportAsync(report);
        }

        public Task<Report?> GetReportAsync(int id) => _repository.GetReportAsync(id);
        public Task<List<Report>> GetReportsAsync(string? targetType) => _repository.GetReportsAsync(targetType);
        public Task<Dictionary<string, int>> CountNewByTypeAsync() => _repository.CountNewByTypeAsync();
        public Task<bool> HasOpenReportAsync(string authorUserId, string targetType, int targetId) => _repository.HasOpenReportAsync(authorUserId, targetType, targetId);
        public Task<int> CountReportsSinceAsync(string authorUserId, DateTime sinceUtc) => _repository.CountReportsSinceAsync(authorUserId, sinceUtc);
        public Task<Report?> SetStatusAsync(int id, string status, string? adminNote, string? handledBy) => _repository.SetStatusAsync(id, status, adminNote, handledBy);
        public Task<Report?> UpdateReportAsync(int id, string reason, string justification, string? adminNote) => _repository.UpdateReportAsync(id, reason, justification, adminNote);
        public Task<bool> DeleteReportAsync(int id) => _repository.DeleteReportAsync(id);
    }
}
