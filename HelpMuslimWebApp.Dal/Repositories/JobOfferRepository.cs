using System.Linq.Expressions;
using HelpMuslimWebApp.Dal.Entities;
using HelpMuslimWebApp.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Models = HelpMuslimWebApp.Domain.Models;

namespace HelpMuslimWebApp.Dal.Repositories
{
    public class JobOfferRepository : IJobOfferRepository
    {
        private readonly HelpMuslimWebAppDatabaseContext _context;

        public JobOfferRepository(HelpMuslimWebAppDatabaseContext context)
        {
            _context = context;
        }

        public async Task<Models.PagedResult<Models.JobOffer>> GetJobOffersAsync(Models.OfferFilter filter)
        {
            var query = Filter(filter);

            var total = await query.CountAsync();
            var items = await Order(query, filter)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(Projection)
                .ToListAsync();

            return new Models.PagedResult<Models.JobOffer> { Items = items, Total = total, Page = filter.Page, PageSize = filter.PageSize };
        }

        // Toutes les offres correspondant aux filtres, sans pagination (tri par correspondance côté domaine)
        public async Task<IEnumerable<Models.JobOffer>> GetFilteredJobOffersAsync(Models.OfferFilter filter)
        {
            return await Order(Filter(filter), filter)
                .Select(Projection)
                .ToListAsync();
        }

        // Listes publiques : offres sponsorisées en tête, à tour de rôle (ordre tiré chaque jour, le même toute la journée pour
        // une pagination fiable), puis les plus récentes
        private static IOrderedQueryable<JobOffer> Order(IQueryable<JobOffer> query, Models.OfferFilter filter)
        {
            if (!filter.OnlyOpen) return query.OrderByDescending(o => o.CreatedAt);

            var now = DateTime.UtcNow;
            var (factor, shift) = SponsoredRotation(now);
            return query.OrderByDescending(o => o.SponsoredUntil > now)
                .ThenBy(o => o.SponsoredUntil > now ? ((long)o.Id * factor + shift) % RotationModulus : 0L)
                .ThenByDescending(o => o.CreatedAt);
        }

        // Rotation des offres sponsorisées : permutation (id × facteur + décalage) modulo un nombre premier, facteur et décalage
        // tirés du jour (heure de Paris) : chaque jour un ordre différent, sans rien enregistrer
        private const long RotationModulus = 1_000_003;

        private static (long Factor, long Shift) SponsoredRotation(DateTime utcNow)
        {
            TimeZoneInfo zone;
            try { zone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Paris"); }
            catch (TimeZoneNotFoundException) { zone = TimeZoneInfo.Utc; }
            long day = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(utcNow, zone)).DayNumber;
            return (1 + day * 2_654_435_761 % (RotationModulus - 1), day * 40_503 % RotationModulus);
        }

        // Statistiques : une consultation de plus
        public Task IncrementViewsAsync(int jobOfferId) =>
            _context.JobOffers.Where(o => o.Id == jobOfferId).ExecuteUpdateAsync(s => s.SetProperty(o => o.ViewsCount, o => o.ViewsCount + 1));

        private IQueryable<JobOffer> Filter(Models.OfferFilter filter)
        {
            var query = _context.JobOffers.AsNoTracking();

            // Fil public : offres ouvertes, non masquées, d'une entreprise active
            if (filter.OnlyOpen) query = query.Where(o => o.IsOpen == true && o.IsHidden != true && o.RecruiterProfile!.IsDisabled != true);
            // Recruteurs de test : absents des listes publiques
            if (filter.OnlyOpen && !filter.IncludeTestAccounts) query = query.Where(o => !o.RecruiterProfile!.IsTestAccount);
            if (filter.RecruiterProfileId.HasValue) query = query.Where(o => o.RecruiterProfileId == filter.RecruiterProfileId);
            if (filter.DomainId.HasValue) query = query.Where(o => o.DomainId == filter.DomainId);
            if (filter.ProfessionId.HasValue) query = query.Where(o => o.ProfessionId == filter.ProfessionId);
            if (!string.IsNullOrWhiteSpace(filter.ContractType)) query = query.Where(o => o.ContractType == filter.ContractType);
            // Recherches insensibles à la casse et aux accents
            if (!string.IsNullOrWhiteSpace(filter.City))
            {
                var cityPattern = Models.SearchText.Pattern(filter.City);
                query = query.Where(o => EF.Functions.ILike(EF.Functions.Unaccent(o.City!), cityPattern));
            }
            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var pattern = Models.SearchText.Pattern(filter.Search);
                query = query.Where(o =>
                    EF.Functions.ILike(EF.Functions.Unaccent(o.Title!), pattern) ||
                    EF.Functions.ILike(EF.Functions.Unaccent(o.Description!), pattern) ||
                    EF.Functions.ILike(EF.Functions.Unaccent(o.RecruiterProfile!.CompanyName!), pattern) ||
                    EF.Functions.ILike(EF.Functions.Unaccent(o.Profession!.Name!), pattern));
            }
            return query;
        }

        public async Task<Models.JobOffer?> GetJobOfferByIdAsync(int id)
        {
            return await _context.JobOffers.AsNoTracking().Where(o => o.Id == id).Select(Projection).FirstOrDefaultAsync();
        }

        public async Task<Models.JobOffer> AddJobOfferAsync(Models.JobOffer model)
        {
            var entity = new JobOffer
            {
                RecruiterProfileId = model.RecruiterProfileId,
                IsOpen = model.IsOpen ?? true,
                CreatedAt = model.CreatedAt ?? DateTime.UtcNow,
            };
            Apply(entity, model);
            _context.JobOffers.Add(entity);
            await _context.SaveChangesAsync();
            return (await GetJobOfferByIdAsync(entity.Id))!;
        }

        public async Task<Models.JobOffer?> UpdateJobOfferAsync(Models.JobOffer model)
        {
            var entity = await _context.JobOffers.Include(o => o.Skills).FirstOrDefaultAsync(o => o.Id == model.Id);
            if (entity == null) return null;

            _context.JobOfferSkills.RemoveRange(entity.Skills);
            entity.Skills.Clear();
            Apply(entity, model);
            await _context.SaveChangesAsync();
            return await GetJobOfferByIdAsync(entity.Id);
        }

        public async Task<Models.FilledOffersStats> GetFilledOffersStatsAsync(int take)
        {
            return new Models.FilledOffersStats
            {
                // Recruteurs de test exclus (page d'accueil publique)
                Count = await _context.OfferFills.CountAsync(f => !f.RecruiterProfile!.IsTestAccount),
                Recent = await _context.OfferFills.AsNoTracking()
                    .Where(f => !f.RecruiterProfile!.IsTestAccount)
                    .OrderByDescending(f => f.FilledAt)
                    .Take(take)
                    .Select(f => new Models.FilledOffer
                    {
                        JobOfferId = f.JobOfferId,
                        Title = f.Title,
                        CompanyName = f.CompanyName,
                        CompanyLogoPath = f.RecruiterProfile!.LogoPath,
                        City = f.City,
                        ContractType = f.ContractType,
                        FilledAt = f.FilledAt,
                    })
                    .ToListAsync(),
            };
        }

        public async Task<Models.JobOffer?> SetJobOfferHiddenAsync(int id, bool isHidden)
        {
            var entity = await _context.JobOffers.FirstOrDefaultAsync(o => o.Id == id);
            if (entity == null) return null;

            entity.IsHidden = isHidden;
            await _context.SaveChangesAsync();
            return await GetJobOfferByIdAsync(id);
        }

        public async Task<Models.JobOffer?> SetJobOfferOpenAsync(int id, bool isOpen, bool isFilled)
        {
            var entity = await _context.JobOffers.FirstOrDefaultAsync(o => o.Id == id);
            if (entity == null) return null;

            entity.IsOpen = isOpen;
            entity.ClosedAt = isOpen ? null : DateTime.UtcNow;
            // Pourvue : uniquement à la fermeture ; une offre rouverte ne l'est plus
            entity.IsFilled = !isOpen && isFilled;
            entity.FilledAt = entity.IsFilled == true ? DateTime.UtcNow : null;

            // Compteur cumulatif : une entrée par offre pourvue, conservée même si l'offre est rouverte ou supprimée
            if (entity.IsFilled == true && !await _context.OfferFills.AnyAsync(f => f.JobOfferId == id))
            {
                var company = await _context.RecruiterProfiles.Where(r => r.Id == entity.RecruiterProfileId).Select(r => r.CompanyName).FirstOrDefaultAsync();
                _context.OfferFills.Add(new OfferFill
                {
                    JobOfferId = id,
                    RecruiterProfileId = entity.RecruiterProfileId,
                    Title = entity.Title,
                    CompanyName = company,
                    City = entity.City,
                    ContractType = entity.ContractType,
                    FilledAt = entity.FilledAt,
                });
            }

            await _context.SaveChangesAsync();
            return await GetJobOfferByIdAsync(id);
        }

        // ---------- Offres favorites des candidats ----------

        public async Task<IEnumerable<Models.JobOffer>> GetFavoriteOffersAsync(int candidateProfileId)
        {
            return await _context.OfferFavorites.AsNoTracking()
                .Where(f => f.CandidateProfileId == candidateProfileId && f.JobOffer!.IsHidden != true && f.JobOffer.RecruiterProfile!.IsDisabled != true)
                .OrderByDescending(f => f.CreatedAt)
                .Select(f => f.JobOffer!)
                .Select(Projection)
                .ToListAsync();
        }

        public Task<bool> OfferFavoriteExistsAsync(int candidateProfileId, int jobOfferId) =>
            _context.OfferFavorites.AnyAsync(f => f.CandidateProfileId == candidateProfileId && f.JobOfferId == jobOfferId);

        public async Task AddOfferFavoriteAsync(int candidateProfileId, int jobOfferId)
        {
            if (await OfferFavoriteExistsAsync(candidateProfileId, jobOfferId)) return;
            _context.OfferFavorites.Add(new OfferFavorite { CandidateProfileId = candidateProfileId, JobOfferId = jobOfferId, CreatedAt = DateTime.UtcNow });
            await _context.SaveChangesAsync();
        }

        public async Task<bool> DeleteOfferFavoriteAsync(int candidateProfileId, int jobOfferId) =>
            await _context.OfferFavorites.Where(f => f.CandidateProfileId == candidateProfileId && f.JobOfferId == jobOfferId).ExecuteDeleteAsync() > 0;

        public Task<int> DeleteOfferFillsAsync(int recruiterProfileId) =>
            _context.OfferFills.Where(f => f.RecruiterProfileId == recruiterProfileId).ExecuteDeleteAsync();

        public Task<int> MarkOffersInDiscussionAsync(int recruiterProfileId, int candidateProfileId) =>
            _context.JobOffers
                .Where(o => o.RecruiterProfileId == recruiterProfileId && o.IsOpen == true && o.InDiscussionAt == null
                            && o.Applications.Any(a => a.CandidateProfileId == candidateProfileId))
                .ExecuteUpdateAsync(s => s.SetProperty(o => o.InDiscussionAt, DateTime.UtcNow));

        public async Task<bool> DeleteJobOfferAsync(int id)
        {
            var entity = await _context.JobOffers.FirstOrDefaultAsync(o => o.Id == id);
            if (entity == null) return false;

            _context.JobOffers.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        private static void Apply(JobOffer entity, Models.JobOffer model)
        {
            if (model.Title != null) entity.Title = model.Title.Trim();
            if (model.Description != null) entity.Description = model.Description;
            if (model.DomainId != null) entity.DomainId = model.DomainId;
            if (model.ContractType != null) entity.ContractType = model.ContractType;
            // Champs facultatifs : peuvent être vidés
            entity.ProfessionId = model.ProfessionId;
            entity.City = model.City;
            entity.IsRemote = model.IsRemote;
            entity.ExperienceYears = model.ExperienceYears;
            entity.Salary = model.Salary;
            entity.Duration = model.Duration;
            entity.StartDate = model.StartDate;

            // Compétences : sans doublon (casse ignorée) ; une compétence requise n'est pas aussi bonus
            var required = Distinct(model.RequiredSkills);
            var bonus = Distinct(model.BonusSkills).Where(b => !required.Contains(b, StringComparer.OrdinalIgnoreCase));
            foreach (var name in required) entity.Skills.Add(new JobOfferSkill { Name = name, IsRequired = true });
            foreach (var name in bonus) entity.Skills.Add(new JobOfferSkill { Name = name, IsRequired = false });
        }

        private static List<string> Distinct(IEnumerable<string>? skills) =>
            (skills ?? []).Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        private static readonly Expression<Func<JobOffer, Models.JobOffer>> Projection = o => new Models.JobOffer
        {
            Id = o.Id,
            RecruiterProfileId = o.RecruiterProfileId,
            RecruiterUserId = o.RecruiterProfile!.UserId,
            CompanyName = o.RecruiterProfile.CompanyName,
            CompanyLogoPath = o.RecruiterProfile.LogoPath,
            RecruiterFirstName = o.RecruiterProfile.FirstName,
            RecruiterLastName = o.RecruiterProfile.LastName,
            RecruiterJobTitle = o.RecruiterProfile.JobTitle,
            RecruiterPhone = o.RecruiterProfile.IsPhoneVisible == true ? o.RecruiterProfile.Phone : null,
            Title = o.Title,
            Description = o.Description,
            DomainId = o.DomainId,
            DomainName = o.Domain!.Name,
            ProfessionId = o.ProfessionId,
            ProfessionName = o.Profession!.Name,
            ContractType = o.ContractType,
            City = o.City,
            IsRemote = o.IsRemote,
            ExperienceYears = o.ExperienceYears,
            Salary = o.Salary,
            Duration = o.Duration,
            StartDate = o.StartDate,
            RequiredSkills = o.Skills.Where(s => s.IsRequired).OrderBy(s => s.Id).Select(s => s.Name!).ToList(),
            BonusSkills = o.Skills.Where(s => !s.IsRequired).OrderBy(s => s.Id).Select(s => s.Name!).ToList(),
            IsOpen = o.IsOpen,
            CreatedAt = o.CreatedAt,
            ClosedAt = o.ClosedAt,
            IsFilled = o.IsFilled,
            FilledAt = o.FilledAt,
            InDiscussionAt = o.InDiscussionAt,
            IsHidden = o.IsHidden,
            CompanyDisabled = o.RecruiterProfile.IsDisabled == true,
            CompanyIsFounder = o.RecruiterProfile.IsFounder == true,
            RecruiterIsTestAccount = o.RecruiterProfile.IsTestAccount,
            SponsoredUntil = o.SponsoredUntil,
            ViewsCount = o.ViewsCount,
            ApplicationsCount = o.Applications.Count,
        };
    }
}
