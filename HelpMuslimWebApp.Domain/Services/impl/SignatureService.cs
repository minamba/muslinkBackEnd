using HelpMuslimWebApp.Domain.Models;
using HelpMuslimWebApp.Domain.Repositories;

namespace HelpMuslimWebApp.Domain.Services.impl
{
    public class SignatureService : ISignatureService
    {
        private readonly ISignatureRepository _repository;

        public SignatureService(ISignatureRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<SignatureTemplate>> GetTemplatesAsync()
        {
            var templates = (await _repository.GetTemplatesAsync()).ToList();
            if (templates.Count > 0) return templates;

            // Premier passage : le modèle par défaut, qui s'adapte au nom et à la fonction de chaque administrateur
            await _repository.AddTemplateAsync(new SignatureTemplate
            {
                Name = "Signature Muslink",
                Closing = "Cordialement,",
                Tagline = "Trouvez l'opportunité qui vous correspond",
                ShowJobTitle = true,
                ShowEmail = true,
                ShowWebsite = true,
                Color = "#4c1d95",
                IsDefault = true,
            });
            return await _repository.GetTemplatesAsync();
        }

        public Task<SignatureTemplate?> GetTemplateByIdAsync(int id) => _repository.GetTemplateByIdAsync(id);

        public Task<SignatureTemplate> AddTemplateAsync(SignatureTemplate model) => _repository.AddTemplateAsync(model);

        public Task<SignatureTemplate?> UpdateTemplateAsync(SignatureTemplate model) => _repository.UpdateTemplateAsync(model);

        public Task<bool> DeleteTemplateAsync(int id) => _repository.DeleteTemplateAsync(id);

        public Task<IEnumerable<SignatureAssignment>> GetAssignmentsAsync() => _repository.GetAssignmentsAsync();

        public async Task<SignatureAssignment> SaveAssignmentAsync(SignatureAssignment model)
        {
            if (model.TemplateId is int id && await _repository.GetTemplateByIdAsync(id) == null)
                throw new KeyNotFoundException("Modèle de signature introuvable.");
            return await _repository.SaveAssignmentAsync(model);
        }

        public async Task<(SignatureTemplate Template, string? JobTitle)> ResolveAsync(string? userId)
        {
            var templates = (await GetTemplatesAsync()).ToList();
            var assignment = string.IsNullOrEmpty(userId) ? null : await _repository.GetAssignmentAsync(userId);
            var template = templates.FirstOrDefault(t => t.Id == assignment?.TemplateId)
                           ?? templates.FirstOrDefault(t => t.IsDefault)
                           ?? templates[0];
            return (template, assignment?.JobTitle);
        }
    }
}
