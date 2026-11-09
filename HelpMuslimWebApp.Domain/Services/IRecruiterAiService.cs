using HelpMuslimWebApp.Domain.Models;

namespace HelpMuslimWebApp.Domain.Services
{
    public interface IRecruiterAiService
    {
        Task AddAsync(RecruiterAiUsage model);
        Task<int> CountSinceAsync(int recruiterProfileId, IReadOnlyCollection<string> features, DateTime sinceUtc);
    }
}
