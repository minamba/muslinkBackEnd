using HelpMuslimWebApp.Domain.Models;

namespace HelpMuslimWebApp.Domain.Repositories
{
    public interface ISignatureRepository
    {
        Task<IEnumerable<SignatureTemplate>> GetTemplatesAsync();
        Task<SignatureTemplate?> GetTemplateByIdAsync(int id);
        Task<SignatureTemplate> AddTemplateAsync(SignatureTemplate model);
        Task<SignatureTemplate?> UpdateTemplateAsync(SignatureTemplate model);
        Task<bool> DeleteTemplateAsync(int id);

        Task<IEnumerable<SignatureAssignment>> GetAssignmentsAsync();
        Task<SignatureAssignment?> GetAssignmentAsync(string userId);
        Task<SignatureAssignment> SaveAssignmentAsync(SignatureAssignment model);
    }
}
