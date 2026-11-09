using HelpMuslimWebApp.Api.Request;
using HelpMuslimWebApp.Api.ViewModels;

namespace HelpMuslimWebApp.Api.Builders
{
    public interface IProfessionViewModelBuilder
    {
        Task<IEnumerable<ProfessionViewModel>> GetProfessionsAsync(int? domainId);
        Task<ProfessionViewModel?> GetProfessionByIdAsync(int id);
        Task<ProfessionViewModel> AddProfessionAsync(ProfessionRequest model);
        Task<ProfessionViewModel?> UpdateProfessionAsync(ProfessionRequest model);
        Task<bool> DeleteProfessionAsync(int id);
    }
}
