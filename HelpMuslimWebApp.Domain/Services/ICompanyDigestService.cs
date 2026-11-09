using HelpMuslimWebApp.Domain.Models;

namespace HelpMuslimWebApp.Domain.Services
{
    // Récapitulatif quotidien des nouvelles offres des entreprises suivies (candidats gratuits, mode premium actif)
    public interface ICompanyDigestService
    {
        Task AddPendingAsync(int jobOfferId, IReadOnlyCollection<int> candidateProfileIds);
        Task<List<CompanyDigestItem>> GetPendingAsync();
        Task MarkSentAsync(IReadOnlyCollection<int> ids);
    }
}
