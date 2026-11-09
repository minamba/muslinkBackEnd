using HelpMuslimWebApp.Domain.Models;

namespace HelpMuslimWebApp.Domain.Services
{
    public interface IJobDomainService
    {
        Task<IEnumerable<JobDomain>> GetJobDomainsAsync();
        Task<JobDomain?> GetJobDomainByIdAsync(int id);
        Task<JobDomain> AddJobDomainAsync(JobDomain model);
        Task<JobDomain?> UpdateJobDomainAsync(JobDomain model);
        Task<bool> DeleteJobDomainAsync(int id);
    }
}
