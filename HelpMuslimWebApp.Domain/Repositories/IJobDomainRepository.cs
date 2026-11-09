using HelpMuslimWebApp.Domain.Models;

namespace HelpMuslimWebApp.Domain.Repositories
{
    public interface IJobDomainRepository
    {
        Task<IEnumerable<JobDomain>> GetJobDomainsAsync();
        Task<JobDomain?> GetJobDomainByIdAsync(int id);
        Task<JobDomain> AddJobDomainAsync(JobDomain model);
        Task<JobDomain?> UpdateJobDomainAsync(JobDomain model);
        Task<bool> DeleteJobDomainAsync(int id);
    }
}
