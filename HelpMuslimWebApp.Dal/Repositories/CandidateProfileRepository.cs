using HelpMuslimWebApp.Dal.Entities;
using HelpMuslimWebApp.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Models = HelpMuslimWebApp.Domain.Models;

namespace HelpMuslimWebApp.Dal.Repositories
{
    public class CandidateProfileRepository : ICandidateProfileRepository
    {
        private readonly HelpMuslimWebAppDatabaseContext _context;

        public CandidateProfileRepository(HelpMuslimWebAppDatabaseContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Models.CandidateSummary>> GetCandidateProfilesAsync(string? search = null)
        {
            var query = _context.CandidateProfiles.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var pattern = Models.SearchText.Pattern(search);
                query = query.Where(c =>
                    EF.Functions.ILike(EF.Functions.Unaccent(c.FirstName!), pattern) ||
                    EF.Functions.ILike(EF.Functions.Unaccent(c.LastName!), pattern) ||
                    EF.Functions.ILike(c.Email!, pattern) ||
                    EF.Functions.ILike(EF.Functions.Unaccent(c.Headline!), pattern) ||
                    EF.Functions.ILike(EF.Functions.Unaccent(c.DesiredJob!), pattern) ||
                    EF.Functions.ILike(EF.Functions.Unaccent(c.City!), pattern));
            }

            return await query
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new Models.CandidateSummary
                {
                    Id = c.Id, UserId = c.UserId, FirstName = c.FirstName, LastName = c.LastName, Email = c.Email,
                    Headline = c.Headline, City = c.City, Situation = c.Situation, AvailableFrom = c.AvailableFrom, DesiredJob = c.DesiredJob,
                    PhotoPath = c.PhotoPath, CreatedAt = c.CreatedAt,
                    Skills = c.Skills.OrderBy(s => s.Id).Select(s => s.Name!).ToList(),
                    PremiumGrantedUntil = c.PremiumGrantedUntil, PremiumPaidUntil = c.PremiumPaidUntil, IsTestAccount = c.IsTestAccount,
                })
                .ToListAsync();
        }

        public async Task<IEnumerable<Models.CandidateSummary>> GetCandidatesForMatchingAsync()
        {
            // Profils rendus invisibles par le candidat : jamais suggérés aux recruteurs
            return await _context.CandidateProfiles.AsNoTracking()
                .Where(c => c.IsHidden != true)
                .Select(c => new Models.CandidateSummary
                {
                    Id = c.Id, UserId = c.UserId, FirstName = c.FirstName, LastName = c.LastName, Email = c.Email,
                    Headline = c.Headline, City = c.City, Situation = c.Situation, AvailableFrom = c.AvailableFrom, DesiredJob = c.DesiredJob,
                    PhotoPath = c.PhotoPath, CreatedAt = c.CreatedAt,
                    DesiredDomainId = c.DesiredDomainId,
                    DesiredProfessionId = c.DesiredProfessionId,
                    DesiredProfessionName = c.DesiredProfession!.Name,
                    Skills = c.Skills.Select(s => s.Name!).ToList(),
                    ExperienceTitles = c.Experiences.Select(e => e.Title!).ToList(),
                    WishProfessionIds = c.WishProfessionIds != null ? c.WishProfessionIds.ToList() : new List<int>(),
                    PremiumGrantedUntil = c.PremiumGrantedUntil, PremiumPaidUntil = c.PremiumPaidUntil, IsTestAccount = c.IsTestAccount,
                })
                .AsSplitQuery()
                .ToListAsync();
        }

        public async Task<Models.CandidateProfile?> GetCandidateProfileByIdAsync(int id)
        {
            var entity = await WithDetails().FirstOrDefaultAsync(c => c.Id == id);
            return entity == null ? null : ToModel(entity);
        }

        public async Task<Models.CandidateProfile?> GetCandidateProfileByUserIdAsync(string userId)
        {
            var entity = await WithDetails().FirstOrDefaultAsync(c => c.UserId == userId);
            return entity == null ? null : ToModel(entity);
        }

        public async Task<Models.CandidateProfile?> GetCandidateProfileByShareTokenAsync(string token)
        {
            var entity = await WithDetails().FirstOrDefaultAsync(c => c.ShareToken == token);
            return entity == null ? null : ToModel(entity);
        }

        public async Task<bool> UpdateCandidateShareAsync(int id, string? token, bool shareContact)
        {
            var entity = await _context.CandidateProfiles.FirstOrDefaultAsync(c => c.Id == id);
            if (entity == null) return false;

            entity.ShareToken = token;
            entity.ShareContact = shareContact;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SetCandidateHiddenAsync(int id, bool isHidden)
        {
            var entity = await _context.CandidateProfiles.FirstOrDefaultAsync(c => c.Id == id);
            if (entity == null) return false;

            entity.IsHidden = isHidden;
            await _context.SaveChangesAsync();
            return true;
        }

        public Task SetTestAccountAsync(string userId, bool isTestAccount) =>
            _context.CandidateProfiles.Where(p => p.UserId == userId && p.IsTestAccount != isTestAccount)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.IsTestAccount, isTestAccount));

        public async Task SetTestAccountsAsync(IReadOnlyCollection<string> testUserIds)
        {
            await _context.CandidateProfiles.Where(p => p.IsTestAccount && !testUserIds.Contains(p.UserId!))
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.IsTestAccount, false));
            await _context.CandidateProfiles.Where(p => !p.IsTestAccount && testUserIds.Contains(p.UserId!))
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.IsTestAccount, true));
        }

        // Le candidat a postulé à une offre du recruteur, ou ils échangent déjà dans la messagerie
        public async Task<bool> HasRelationWithRecruiterAsync(int candidateProfileId, int recruiterProfileId) =>
            await _context.JobApplications.AnyAsync(a => a.CandidateProfileId == candidateProfileId && a.JobOffer!.RecruiterProfileId == recruiterProfileId)
            || await _context.Conversations.AnyAsync(c => c.CandidateProfileId == candidateProfileId && c.RecruiterProfileId == recruiterProfileId);

        public async Task<Models.CandidateProfile> AddCandidateProfileAsync(Models.CandidateProfile model)
        {
            var entity = new CandidateProfile { UserId = model.UserId, CreatedAt = DateTime.UtcNow };
            Apply(entity, model);
            // Nouveau candidat : disponible tout de suite, jusqu'à ce qu'il règle sa disponibilité
            entity.AvailableFrom ??= DateOnly.FromDateTime(DateTime.Today);
            _context.CandidateProfiles.Add(entity);
            await _context.SaveChangesAsync();
            return (await GetCandidateProfileByIdAsync(entity.Id))!;
        }

        public async Task<Models.CandidateProfile?> UpdateCandidateProfileAsync(Models.CandidateProfile model)
        {
            var entity = await _context.CandidateProfiles
                .Include(c => c.Experiences).Include(c => c.Educations)
                .Include(c => c.Skills).Include(c => c.Languages)
                .AsSplitQuery()
                .FirstOrDefaultAsync(c => c.Id == model.Id);
            if (entity == null) return null;

            // Les listes (expériences, formations…) sont remplacées intégralement
            _context.CandidateExperiences.RemoveRange(entity.Experiences);
            _context.CandidateEducations.RemoveRange(entity.Educations);
            _context.CandidateSkills.RemoveRange(entity.Skills);
            _context.CandidateLanguages.RemoveRange(entity.Languages);
            entity.Experiences.Clear();
            entity.Educations.Clear();
            entity.Skills.Clear();
            entity.Languages.Clear();

            Apply(entity, model);
            entity.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return await GetCandidateProfileByIdAsync(entity.Id);
        }

        public async Task<bool> DeleteCandidateProfileAsync(int id)
        {
            var entity = await _context.CandidateProfiles.FirstOrDefaultAsync(c => c.Id == id);
            if (entity == null) return false;

            _context.CandidateProfiles.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        private IQueryable<CandidateProfile> WithDetails() => _context.CandidateProfiles.AsNoTracking()
            .Include(c => c.DesiredDomain).Include(c => c.DesiredProfession)
            .Include(c => c.Experiences).Include(c => c.Educations)
            .Include(c => c.Skills).Include(c => c.Languages)
            .AsSplitQuery();

        private static void Apply(CandidateProfile entity, Models.CandidateProfile model)
        {
            entity.FirstName = model.FirstName;
            entity.LastName = model.LastName;
            entity.Email = model.Email;
            entity.Phone = model.Phone;
            entity.IsEmailVisible = model.IsEmailVisible;
            entity.IsPhoneVisible = model.IsPhoneVisible;
            entity.IsCvHidden = model.IsCvHidden;
            entity.BirthDate = model.BirthDate;
            entity.City = model.City;
            entity.Headline = model.Headline;
            entity.Situation = model.Situation;
            entity.AvailableFrom = model.AvailableFrom;
            entity.DesiredJob = model.DesiredJob;
            entity.DesiredDomainId = model.DesiredDomainId;
            entity.DesiredProfessionId = model.DesiredProfessionId;
            entity.Summary = model.Summary;
            entity.PhotoPath = model.PhotoPath;
            entity.CvPath = model.CvPath;
            entity.CvFileName = model.CvFileName;
            entity.LinkedinUrl = model.LinkedinUrl;
            entity.WishContractTypes = [.. model.WishContractTypes];
            entity.WishProfessionIds = [.. model.WishProfessionIds];
            entity.WishCities = [.. model.WishCities];
            entity.WishAnywhere = model.WishAnywhere;
            entity.WishRemote = model.WishRemote;
            entity.WishSalaryMin = model.WishSalaryMin;

            foreach (var e in model.Experiences)
                entity.Experiences.Add(new CandidateExperience
                {
                    Title = e.Title, Company = e.Company, City = e.City, StartDate = e.StartDate,
                    EndDate = e.IsCurrent == true ? null : e.EndDate, IsCurrent = e.IsCurrent, Description = e.Description,
                });
            foreach (var e in model.Educations)
                entity.Educations.Add(new CandidateEducation
                {
                    School = e.School, Degree = e.Degree, FieldOfStudy = e.FieldOfStudy,
                    StartDate = e.StartDate, EndDate = e.EndDate, Description = e.Description,
                });
            foreach (var skill in model.Skills.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).Distinct())
                entity.Skills.Add(new CandidateSkill { Name = skill });
            foreach (var l in model.Languages.Where(l => !string.IsNullOrWhiteSpace(l.Name)))
                entity.Languages.Add(new CandidateLanguage { Name = l.Name!.Trim(), Level = l.Level });
        }

        internal static Models.CandidateProfile ToModel(CandidateProfile entity) => new()
        {
            Id = entity.Id,
            UserId = entity.UserId,
            FirstName = entity.FirstName,
            LastName = entity.LastName,
            Email = entity.Email,
            Phone = entity.Phone,
            BirthDate = entity.BirthDate,
            City = entity.City,
            Headline = entity.Headline,
            Situation = entity.Situation,
            AvailableFrom = entity.AvailableFrom,
            DesiredJob = entity.DesiredJob,
            DesiredDomainId = entity.DesiredDomainId,
            DesiredDomainName = entity.DesiredDomain?.Name,
            DesiredProfessionId = entity.DesiredProfessionId,
            DesiredProfessionName = entity.DesiredProfession?.Name,
            Summary = entity.Summary,
            PhotoPath = entity.PhotoPath,
            CvPath = entity.CvPath,
            CvFileName = entity.CvFileName,
            LinkedinUrl = entity.LinkedinUrl,
            ShareToken = entity.ShareToken,
            ShareContact = entity.ShareContact,
            IsHidden = entity.IsHidden,
            IsTestAccount = entity.IsTestAccount,
            IsEmailVisible = entity.IsEmailVisible == true,
            IsPhoneVisible = entity.IsPhoneVisible == true,
            IsCvHidden = entity.IsCvHidden == true,
            WishContractTypes = [.. entity.WishContractTypes ?? []],
            WishProfessionIds = [.. entity.WishProfessionIds ?? []],
            WishCities = [.. entity.WishCities ?? []],
            WishAnywhere = entity.WishAnywhere == true,
            WishRemote = entity.WishRemote == true,
            WishSalaryMin = entity.WishSalaryMin,
            // Premium : modifié uniquement par l'administration et Stripe (jamais par le formulaire du profil)
            PremiumGrantedUntil = entity.PremiumGrantedUntil,
            PremiumPaidUntil = entity.PremiumPaidUntil,
            StripeCustomerId = entity.StripeCustomerId,
            StripeSubscriptionId = entity.StripeSubscriptionId,
            PremiumCancelAtPeriodEnd = entity.PremiumCancelAtPeriodEnd == true,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            // Expériences et formations : la plus récente en premier
            Experiences = entity.Experiences
                .OrderByDescending(e => e.IsCurrent == true).ThenByDescending(e => e.StartDate)
                .Select(e => new Models.CandidateExperience
                {
                    Id = e.Id, Title = e.Title, Company = e.Company, City = e.City, StartDate = e.StartDate,
                    EndDate = e.EndDate, IsCurrent = e.IsCurrent, Description = e.Description,
                }).ToList(),
            Educations = entity.Educations
                .OrderByDescending(e => e.StartDate)
                .Select(e => new Models.CandidateEducation
                {
                    Id = e.Id, School = e.School, Degree = e.Degree, FieldOfStudy = e.FieldOfStudy,
                    StartDate = e.StartDate, EndDate = e.EndDate, Description = e.Description,
                }).ToList(),
            Skills = entity.Skills.OrderBy(s => s.Id).Select(s => s.Name!).ToList(),
            Languages = entity.Languages.OrderBy(l => l.Id)
                .Select(l => new Models.CandidateLanguage { Id = l.Id, Name = l.Name, Level = l.Level }).ToList(),
        };
    }
}
