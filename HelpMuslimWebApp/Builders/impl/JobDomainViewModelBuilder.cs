using AutoMapper;
using HelpMuslimWebApp.Api.Request;
using HelpMuslimWebApp.Api.ViewModels;
using HelpMuslimWebApp.Domain.Models;
using HelpMuslimWebApp.Domain.Services;

namespace HelpMuslimWebApp.Api.Builders.impl
{
    public class JobDomainViewModelBuilder : IJobDomainViewModelBuilder
    {
        private readonly IMapper _mapper;
        private readonly IJobDomainService _jobDomainService;

        public JobDomainViewModelBuilder(IMapper mapper, IJobDomainService jobDomainService)
        {
            _mapper = mapper;
            _jobDomainService = jobDomainService;
        }

        public async Task<IEnumerable<JobDomainViewModel>> GetJobDomainsAsync() =>
            _mapper.Map<IEnumerable<JobDomainViewModel>>(await _jobDomainService.GetJobDomainsAsync());

        public async Task<JobDomainViewModel?> GetJobDomainByIdAsync(int id)
        {
            var domain = await _jobDomainService.GetJobDomainByIdAsync(id);
            return domain == null ? null : _mapper.Map<JobDomainViewModel>(domain);
        }

        public async Task<JobDomainViewModel> AddJobDomainAsync(JobDomainRequest model)
        {
            await EnsureUniqueNameAsync(model.Name!, null);
            var saved = await _jobDomainService.AddJobDomainAsync(_mapper.Map<JobDomain>(model));
            return _mapper.Map<JobDomainViewModel>(saved);
        }

        public async Task<JobDomainViewModel?> UpdateJobDomainAsync(JobDomainRequest model)
        {
            await EnsureUniqueNameAsync(model.Name!, model.Id);
            var saved = await _jobDomainService.UpdateJobDomainAsync(_mapper.Map<JobDomain>(model));
            return saved == null ? null : _mapper.Map<JobDomainViewModel>(saved);
        }

        public Task<bool> DeleteJobDomainAsync(int id) => _jobDomainService.DeleteJobDomainAsync(id);

        private async Task EnsureUniqueNameAsync(string name, int? currentId)
        {
            var existing = await _jobDomainService.GetJobDomainsAsync();
            if (existing.Any(d => d.Id != currentId && string.Equals(d.Name, name.Trim(), StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("Un domaine porte déjà ce nom.");
        }
    }
}
