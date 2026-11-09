using AutoMapper;
using HelpMuslimWebApp.Api.Request;
using HelpMuslimWebApp.Api.ViewModels;
using HelpMuslimWebApp.Domain.Models;
using HelpMuslimWebApp.Domain.Services;

namespace HelpMuslimWebApp.Api.Builders.impl
{
    public class ProfessionViewModelBuilder : IProfessionViewModelBuilder
    {
        private readonly IMapper _mapper;
        private readonly IProfessionService _professionService;
        private readonly IJobDomainService _jobDomainService;

        public ProfessionViewModelBuilder(IMapper mapper, IProfessionService professionService, IJobDomainService jobDomainService)
        {
            _mapper = mapper;
            _professionService = professionService;
            _jobDomainService = jobDomainService;
        }

        public async Task<IEnumerable<ProfessionViewModel>> GetProfessionsAsync(int? domainId) =>
            _mapper.Map<IEnumerable<ProfessionViewModel>>(await _professionService.GetProfessionsAsync(domainId));

        public async Task<ProfessionViewModel?> GetProfessionByIdAsync(int id)
        {
            var profession = await _professionService.GetProfessionByIdAsync(id);
            return profession == null ? null : _mapper.Map<ProfessionViewModel>(profession);
        }

        public async Task<ProfessionViewModel> AddProfessionAsync(ProfessionRequest model)
        {
            await ValidateAsync(model);
            var saved = await _professionService.AddProfessionAsync(_mapper.Map<Profession>(model));
            return (await GetProfessionByIdAsync(saved.Id))!;
        }

        public async Task<ProfessionViewModel?> UpdateProfessionAsync(ProfessionRequest model)
        {
            await ValidateAsync(model);
            var saved = await _professionService.UpdateProfessionAsync(_mapper.Map<Profession>(model));
            return saved == null ? null : _mapper.Map<ProfessionViewModel>(saved);
        }

        public Task<bool> DeleteProfessionAsync(int id) => _professionService.DeleteProfessionAsync(id);

        private async Task ValidateAsync(ProfessionRequest model)
        {
            if (await _jobDomainService.GetJobDomainByIdAsync(model.DomainId!.Value) == null)
                throw new ArgumentException("Le domaine sélectionné n'existe pas.");

            var siblings = await _professionService.GetProfessionsAsync(model.DomainId);
            if (siblings.Any(p => p.Id != model.Id && string.Equals(p.Name, model.Name!.Trim(), StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("Ce métier existe déjà dans ce domaine.");
        }
    }
}
