using HelpMuslimWebApp.Dal.Entities;
using HelpMuslimWebApp.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Models = HelpMuslimWebApp.Domain.Models;

namespace HelpMuslimWebApp.Dal.Repositories
{
    public class SignatureRepository : ISignatureRepository
    {
        private readonly HelpMuslimWebAppDatabaseContext _context;

        public SignatureRepository(HelpMuslimWebAppDatabaseContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Models.SignatureTemplate>> GetTemplatesAsync()
        {
            var entities = await _context.SignatureTemplates.AsNoTracking()
                .Include(t => t.Assignments)
                .OrderByDescending(t => t.IsDefault).ThenBy(t => t.Name)
                .ToListAsync();
            return entities.Select(ToModel);
        }

        public async Task<Models.SignatureTemplate?> GetTemplateByIdAsync(int id)
        {
            var entity = await _context.SignatureTemplates.AsNoTracking().Include(t => t.Assignments).FirstOrDefaultAsync(t => t.Id == id);
            return entity == null ? null : ToModel(entity);
        }

        public async Task<Models.SignatureTemplate> AddTemplateAsync(Models.SignatureTemplate model)
        {
            var entity = new SignatureTemplate { CreatedAt = DateTime.UtcNow };
            Apply(entity, model);
            if (model.IsDefault) await ClearDefaultAsync();
            _context.SignatureTemplates.Add(entity);
            await _context.SaveChangesAsync();
            return (await GetTemplateByIdAsync(entity.Id))!;
        }

        public async Task<Models.SignatureTemplate?> UpdateTemplateAsync(Models.SignatureTemplate model)
        {
            var entity = await _context.SignatureTemplates.FirstOrDefaultAsync(t => t.Id == model.Id);
            if (entity == null) return null;

            // Le modèle par défaut le reste tant qu'un autre ne prend pas sa place
            if (entity.IsDefault && !model.IsDefault) model.IsDefault = true;
            if (model.IsDefault && !entity.IsDefault) await ClearDefaultAsync();
            Apply(entity, model);
            await _context.SaveChangesAsync();
            return await GetTemplateByIdAsync(entity.Id);
        }

        public async Task<bool> DeleteTemplateAsync(int id)
        {
            var entity = await _context.SignatureTemplates.FirstOrDefaultAsync(t => t.Id == id);
            if (entity == null) return false;
            if (entity.IsDefault)
                throw new InvalidOperationException("Le modèle par défaut ne peut pas être supprimé : choisissez d'abord un autre modèle par défaut.");

            _context.SignatureTemplates.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<Models.SignatureAssignment>> GetAssignmentsAsync() =>
            (await _context.SignatureAssignments.AsNoTracking().ToListAsync()).Select(ToModel);

        public async Task<Models.SignatureAssignment?> GetAssignmentAsync(string userId)
        {
            var entity = await _context.SignatureAssignments.AsNoTracking().FirstOrDefaultAsync(a => a.UserId == userId);
            return entity == null ? null : ToModel(entity);
        }

        public async Task<Models.SignatureAssignment> SaveAssignmentAsync(Models.SignatureAssignment model)
        {
            var entity = await _context.SignatureAssignments.FirstOrDefaultAsync(a => a.UserId == model.UserId);
            if (entity == null)
            {
                entity = new SignatureAssignment { UserId = model.UserId };
                _context.SignatureAssignments.Add(entity);
            }
            entity.TemplateId = model.TemplateId;
            entity.JobTitle = string.IsNullOrWhiteSpace(model.JobTitle) ? null : model.JobTitle.Trim();
            entity.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return ToModel(entity);
        }

        private async Task ClearDefaultAsync()
        {
            foreach (var other in await _context.SignatureTemplates.Where(t => t.IsDefault).ToListAsync())
                other.IsDefault = false;
        }

        private static void Apply(SignatureTemplate entity, Models.SignatureTemplate model)
        {
            entity.Name = model.Name?.Trim();
            entity.Closing = Clean(model.Closing);
            entity.Tagline = Clean(model.Tagline);
            entity.ShowJobTitle = model.ShowJobTitle;
            entity.ShowEmail = model.ShowEmail;
            entity.ShowWebsite = model.ShowWebsite;
            entity.Phone = Clean(model.Phone);
            entity.Color = Clean(model.Color);
            entity.IsDefault = model.IsDefault;
        }

        private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static Models.SignatureTemplate ToModel(SignatureTemplate entity) => new()
        {
            Id = entity.Id,
            Name = entity.Name,
            Closing = entity.Closing,
            Tagline = entity.Tagline,
            ShowJobTitle = entity.ShowJobTitle,
            ShowEmail = entity.ShowEmail,
            ShowWebsite = entity.ShowWebsite,
            Phone = entity.Phone,
            Color = entity.Color,
            IsDefault = entity.IsDefault,
            AssignedCount = entity.Assignments.Count(a => a.TemplateId == entity.Id),
        };

        private static Models.SignatureAssignment ToModel(SignatureAssignment entity) => new()
        {
            UserId = entity.UserId,
            TemplateId = entity.TemplateId,
            JobTitle = entity.JobTitle,
        };
    }
}
