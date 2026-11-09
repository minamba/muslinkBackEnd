using HelpMuslimWebApp.Domain.Models;

namespace HelpMuslimWebApp.Domain.Repositories
{
    public interface IProfessionRepository
    {
        Task<IEnumerable<Profession>> GetProfessionsAsync(int? domainId = null);
        Task<Profession?> GetProfessionByIdAsync(int id);
        Task<Profession> AddProfessionAsync(Profession model);
        Task<Profession?> UpdateProfessionAsync(Profession model);
        Task<bool> DeleteProfessionAsync(int id);
    }
}
