using HelpMuslimWebApp.Domain.Models;

namespace HelpMuslimWebApp.Domain.Repositories
{
    public interface IRecruiterAiRepository
    {
        Task AddAsync(RecruiterAiUsage model);
        // Appels du recruteur à ces outils depuis cette date (UTC) : quota du mois
        Task<int> CountSinceAsync(int recruiterProfileId, IReadOnlyCollection<string> features, DateTime sinceUtc);
    }
}
