using HelpMuslimWebApp.Domain.Models;
using HelpMuslimWebApp.Domain.Repositories;

namespace HelpMuslimWebApp.Domain.Services.impl
{
    public class ProfessionService : IProfessionService
    {
        private readonly IProfessionRepository _repository;

        public ProfessionService(IProfessionRepository repository)
        {
            _repository = repository;
        }

        public Task<IEnumerable<Profession>> GetProfessionsAsync(int? domainId = null) => _repository.GetProfessionsAsync(domainId);
        public Task<Profession?> GetProfessionByIdAsync(int id) => _repository.GetProfessionByIdAsync(id);
        public Task<Profession> AddProfessionAsync(Profession model) => _repository.AddProfessionAsync(model);
        public Task<Profession?> UpdateProfessionAsync(Profession model) => _repository.UpdateProfessionAsync(model);
        public Task<bool> DeleteProfessionAsync(int id) => _repository.DeleteProfessionAsync(id);
    }
}
