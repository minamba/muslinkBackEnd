using HelpMuslimWebApp.Dal.Entities;
using HelpMuslimWebApp.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Models = HelpMuslimWebApp.Domain.Models;

namespace HelpMuslimWebApp.Dal.Repositories
{
    public class JobDomainRepository : IJobDomainRepository
    {
        private readonly HelpMuslimWebAppDatabaseContext _context;

        public JobDomainRepository(HelpMuslimWebAppDatabaseContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Models.JobDomain>> GetJobDomainsAsync()
        {
            var entities = await _context.JobDomains.AsNoTracking()
                .Include(d => d.Professions)
                .OrderBy(d => d.Name)
                .ToListAsync();
            return entities.Select(ToModel);
        }

        public async Task<Models.JobDomain?> GetJobDomainByIdAsync(int id)
        {
            var entity = await _context.JobDomains.AsNoTracking()
                .Include(d => d.Professions)
                .FirstOrDefaultAsync(d => d.Id == id);
            return entity == null ? null : ToModel(entity);
        }

        public async Task<Models.JobDomain> AddJobDomainAsync(Models.JobDomain model)
        {
            var entity = new JobDomain { Name = model.Name?.Trim(), Description = model.Description };
            _context.JobDomains.Add(entity);
            await _context.SaveChangesAsync();
            model.Id = entity.Id;
            return model;
        }

        public async Task<Models.JobDomain?> UpdateJobDomainAsync(Models.JobDomain model)
        {
            var entity = await _context.JobDomains.FirstOrDefaultAsync(d => d.Id == model.Id);
            if (entity == null) return null;

            if (model.Name != null) entity.Name = model.Name.Trim();
            if (model.Description != null) entity.Description = model.Description;

            await _context.SaveChangesAsync();
            return await GetJobDomainByIdAsync(entity.Id);
        }

        public async Task<bool> DeleteJobDomainAsync(int id)
        {
            var entity = await _context.JobDomains.FirstOrDefaultAsync(d => d.Id == id);
            if (entity == null) return false;

            if (await _context.JobOffers.AnyAsync(o => o.DomainId == id))
                throw new InvalidOperationException("Ce domaine est utilisé par des offres et ne peut pas être supprimé.");

            _context.JobDomains.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        private static Models.JobDomain ToModel(JobDomain entity) => new()
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            Professions = entity.Professions
                .OrderBy(p => p.Name)
                .Select(p => new Models.Profession { Id = p.Id, DomainId = p.DomainId, DomainName = entity.Name, Name = p.Name })
                .ToList(),
        };
    }
}
