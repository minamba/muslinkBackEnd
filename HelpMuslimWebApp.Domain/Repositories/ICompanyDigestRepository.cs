using HelpMuslimWebApp.Domain.Models;

namespace HelpMuslimWebApp.Domain.Repositories
{
    public interface ICompanyDigestRepository
    {
        // Une ligne par candidat et par offre (déjà en attente : ignorée)
        Task AddPendingAsync(int jobOfferId, IReadOnlyCollection<int> candidateProfileIds);
        Task<List<CompanyDigestItem>> GetPendingAsync();
        Task MarkSentAsync(IReadOnlyCollection<int> ids);
    }
}
