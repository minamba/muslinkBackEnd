using HelpMuslimWebApp.Domain.Models;
using HelpMuslimWebApp.Domain.Repositories;

namespace HelpMuslimWebApp.Domain.Services.impl
{
    public class JobDomainService : IJobDomainService
    {
        private readonly IJobDomainRepository _repository;

        public JobDomainService(IJobDomainRepository repository)
        {
            _repository = repository;
        }

        public Task<IEnumerable<JobDomain>> GetJobDomainsAsync() => _repository.GetJobDomainsAsync();
        public Task<JobDomain?> GetJobDomainByIdAsync(int id) => _repository.GetJobDomainByIdAsync(id);
        public Task<JobDomain> AddJobDomainAsync(JobDomain model) => _repository.AddJobDomainAsync(model);
        public Task<JobDomain?> UpdateJobDomainAsync(JobDomain model) => _repository.UpdateJobDomainAsync(model);
        public Task<bool> DeleteJobDomainAsync(int id) => _repository.DeleteJobDomainAsync(id);
    }
}
