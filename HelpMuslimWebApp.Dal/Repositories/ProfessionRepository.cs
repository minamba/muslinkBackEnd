using HelpMuslimWebApp.Dal.Entities;
using HelpMuslimWebApp.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Models = HelpMuslimWebApp.Domain.Models;

namespace HelpMuslimWebApp.Dal.Repositories
{
    public class ProfessionRepository : IProfessionRepository
    {
        private readonly HelpMuslimWebAppDatabaseContext _context;

        public ProfessionRepository(HelpMuslimWebAppDatabaseContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Models.Profession>> GetProfessionsAsync(int? domainId = null)
        {
            var query = _context.Professions.AsNoTracking();
            if (domainId.HasValue) query = query.Where(p => p.DomainId == domainId);

            return await query
                .OrderBy(p => p.Name)
                .Select(p => new Models.Profession { Id = p.Id, DomainId = p.DomainId, DomainName = p.Domain!.Name, Name = p.Name })
                .ToListAsync();
        }

        public async Task<Models.Profession?> GetProfessionByIdAsync(int id)
        {
            return await _context.Professions.AsNoTracking()
                .Where(p => p.Id == id)
                .Select(p => new Models.Profession { Id = p.Id, DomainId = p.DomainId, DomainName = p.Domain!.Name, Name = p.Name })
                .FirstOrDefaultAsync();
        }

        public async Task<Models.Profession> AddProfessionAsync(Models.Profession model)
        {
            var entity = new Profession { DomainId = model.DomainId, Name = model.Name?.Trim() };
            _context.Professions.Add(entity);
            await _context.SaveChangesAsync();
            model.Id = entity.Id;
            return model;
        }

        public async Task<Models.Profession?> UpdateProfessionAsync(Models.Profession model)
        {
            var entity = await _context.Professions.FirstOrDefaultAsync(p => p.Id == model.Id);
            if (entity == null) return null;

            if (model.Name != null) entity.Name = model.Name.Trim();
            if (model.DomainId != null) entity.DomainId = model.DomainId;

            await _context.SaveChangesAsync();
            return await GetProfessionByIdAsync(entity.Id);
        }

        public async Task<bool> DeleteProfessionAsync(int id)
        {
            var entity = await _context.Professions.FirstOrDefaultAsync(p => p.Id == id);
            if (entity == null) return false;

            if (await _context.JobOffers.AnyAsync(o => o.ProfessionId == id))
                throw new InvalidOperationException("Ce métier est utilisé par des offres et ne peut pas être supprimé.");

            _context.Professions.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
