using HelpMuslimWebApp.Api.Request;
using HelpMuslimWebApp.Api.ViewModels;

namespace HelpMuslimWebApp.Api.Builders
{
    public interface IJobDomainViewModelBuilder
    {
        Task<IEnumerable<JobDomainViewModel>> GetJobDomainsAsync();
        Task<JobDomainViewModel?> GetJobDomainByIdAsync(int id);
        Task<JobDomainViewModel> AddJobDomainAsync(JobDomainRequest model);
        Task<JobDomainViewModel?> UpdateJobDomainAsync(JobDomainRequest model);
        Task<bool> DeleteJobDomainAsync(int id);
    }
}
