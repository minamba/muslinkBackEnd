using HelpMuslimWebApp.Domain.Models;
using HelpMuslimWebApp.Domain.Repositories;

namespace HelpMuslimWebApp.Domain.Services.impl
{
    public class RecruiterAiService : IRecruiterAiService
    {
        private readonly IRecruiterAiRepository _repository;

        public RecruiterAiService(IRecruiterAiRepository repository)
        {
            _repository = repository;
        }

        public Task AddAsync(RecruiterAiUsage model)
        {
            model.CreatedAt = DateTime.UtcNow;
            return _repository.AddAsync(model);
        }

        public Task<int> CountSinceAsync(int recruiterProfileId, IReadOnlyCollection<string> features, DateTime sinceUtc) =>
            _repository.CountSinceAsync(recruiterProfileId, features, sinceUtc);
    }
}
