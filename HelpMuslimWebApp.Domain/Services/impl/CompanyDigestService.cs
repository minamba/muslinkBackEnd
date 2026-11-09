using HelpMuslimWebApp.Domain.Models;
using HelpMuslimWebApp.Domain.Repositories;

namespace HelpMuslimWebApp.Domain.Services.impl
{
    public class CompanyDigestService : ICompanyDigestService
    {
        private readonly ICompanyDigestRepository _repository;

        public CompanyDigestService(ICompanyDigestRepository repository)
        {
            _repository = repository;
        }

        public Task AddPendingAsync(int jobOfferId, IReadOnlyCollection<int> candidateProfileIds) =>
            candidateProfileIds.Count == 0 ? Task.CompletedTask : _repository.AddPendingAsync(jobOfferId, candidateProfileIds);

        public Task<List<CompanyDigestItem>> GetPendingAsync() => _repository.GetPendingAsync();

        public Task MarkSentAsync(IReadOnlyCollection<int> ids) => ids.Count == 0 ? Task.CompletedTask : _repository.MarkSentAsync(ids);
    }
}
