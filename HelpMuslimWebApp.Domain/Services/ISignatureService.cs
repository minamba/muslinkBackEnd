using HelpMuslimWebApp.Domain.Models;

namespace HelpMuslimWebApp.Domain.Services
{
    public interface ISignatureService
    {
        // Les modèles, le modèle par défaut en premier (créé s'il n'en existe aucun)
        Task<IEnumerable<SignatureTemplate>> GetTemplatesAsync();
        Task<SignatureTemplate?> GetTemplateByIdAsync(int id);
        Task<SignatureTemplate> AddTemplateAsync(SignatureTemplate model);
        Task<SignatureTemplate?> UpdateTemplateAsync(SignatureTemplate model);
        Task<bool> DeleteTemplateAsync(int id);

        Task<IEnumerable<SignatureAssignment>> GetAssignmentsAsync();
        Task<SignatureAssignment> SaveAssignmentAsync(SignatureAssignment model);

        // Ce que signe un administrateur : son modèle, ou le modèle par défaut ; et sa fonction
        Task<(SignatureTemplate Template, string? JobTitle)> ResolveAsync(string? userId);
    }
}
