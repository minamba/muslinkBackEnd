using System.Linq.Expressions;
using HelpMuslimWebApp.Dal.Entities;
using HelpMuslimWebApp.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Models = HelpMuslimWebApp.Domain.Models;

namespace HelpMuslimWebApp.Dal.Repositories
{
    public class JobApplicationRepository : IJobApplicationRepository
    {
        private readonly HelpMuslimWebAppDatabaseContext _context;

        public JobApplicationRepository(HelpMuslimWebAppDatabaseContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Models.JobApplication>> GetJobApplicationsAsync(int? jobOfferId = null, int? candidateProfileId = null)
        {
            var query = _context.JobApplications.AsNoTracking();
            if (jobOfferId.HasValue) query = query.Where(a => a.JobOfferId == jobOfferId);
            if (candidateProfileId.HasValue) query = query.Where(a => a.CandidateProfileId == candidateProfileId);

            return await query.OrderByDescending(a => a.CreatedAt).Select(Projection).ToListAsync();
        }

        // « Mes candidatures » : offres ouvertes, et offres fermées depuis moins longtemps que closedSince,
        // sauf celles que le candidat a retirées de sa liste (elles réapparaissent si l'offre est rouverte)
        public async Task<IEnumerable<Models.JobApplication>> GetCandidateApplicationsAsync(int candidateProfileId, DateTime closedSince)
        {
            return await _context.JobApplications.AsNoTracking()
                .Where(a => a.CandidateProfileId == candidateProfileId)
                .Where(a => a.JobOffer!.IsOpen == true
                            || (a.HiddenByCandidateAt == null && (a.JobOffer.ClosedAt == null || a.JobOffer.ClosedAt >= closedSince)))
                .OrderByDescending(a => a.CreatedAt)
                .Select(Projection)
                .ToListAsync();
        }

        // Copies de CV jointes aux candidatures d'une offre, d'un candidat ou d'un recruteur (fichiers à supprimer avec elles)
        public async Task<IEnumerable<string>> GetApplicationCvPathsAsync(int? jobOfferId = null, int? candidateProfileId = null, int? recruiterProfileId = null)
        {
            var query = _context.JobApplications.AsNoTracking().Where(a => a.CvPath != null);
            if (jobOfferId.HasValue) query = query.Where(a => a.JobOfferId == jobOfferId);
            if (candidateProfileId.HasValue) query = query.Where(a => a.CandidateProfileId == candidateProfileId);
            if (recruiterProfileId.HasValue) query = query.Where(a => a.JobOffer!.RecruiterProfileId == recruiterProfileId);
            return await query.Select(a => a.CvPath!).ToListAsync();
        }

        public async Task<bool> HideJobApplicationAsync(int id) =>
            await _context.JobApplications.Where(a => a.Id == id)
                .ExecuteUpdateAsync(s => s.SetProperty(a => a.HiddenByCandidateAt, DateTime.UtcNow)) > 0;

        public async Task<Models.JobApplication?> GetJobApplicationByIdAsync(int id)
        {
            return await _context.JobApplications.AsNoTracking().Where(a => a.Id == id).Select(Projection).FirstOrDefaultAsync();
        }

        public Task<bool> JobApplicationExistsAsync(int jobOfferId, int candidateProfileId)
        {
            return _context.JobApplications.AnyAsync(a => a.JobOfferId == jobOfferId && a.CandidateProfileId == candidateProfileId);
        }

        public async Task<Models.JobApplication> AddJobApplicationAsync(Models.JobApplication model)
        {
            var entity = new JobApplication
            {
                JobOfferId = model.JobOfferId,
                CandidateProfileId = model.CandidateProfileId,
                CoverLetter = model.CoverLetter?.Trim(),
                CvPath = model.CvPath,
                CvFileName = model.CvFileName,
                CreatedAt = model.CreatedAt ?? DateTime.UtcNow,
            };
            _context.JobApplications.Add(entity);
            await _context.SaveChangesAsync();
            return (await GetJobApplicationByIdAsync(entity.Id))!;
        }

        public async Task<bool> DeleteJobApplicationAsync(int id)
        {
            var entity = await _context.JobApplications.FirstOrDefaultAsync(a => a.Id == id);
            if (entity == null) return false;

            _context.JobApplications.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        private static readonly Expression<Func<JobApplication, Models.JobApplication>> Projection = a => new Models.JobApplication
        {
            Id = a.Id,
            JobOfferId = a.JobOfferId,
            CandidateProfileId = a.CandidateProfileId,
            CoverLetter = a.CoverLetter,
            CreatedAt = a.CreatedAt,
            CvPath = a.CvPath,
            CvFileName = a.CvFileName,
            OfferTitle = a.JobOffer!.Title,
            OfferContractType = a.JobOffer.ContractType,
            OfferIsOpen = a.JobOffer.IsOpen,
            OfferCreatedAt = a.JobOffer.CreatedAt,
            OfferClosedAt = a.JobOffer.ClosedAt,
            OfferInDiscussionAt = a.JobOffer.InDiscussionAt,
            OfferIsFilled = a.JobOffer.IsFilled,
            RecruiterProfileId = a.JobOffer.RecruiterProfileId,
            CompanyName = a.JobOffer.RecruiterProfile!.CompanyName,
            CompanyLogoPath = a.JobOffer.RecruiterProfile.LogoPath,
            Candidate = new Models.CandidateSummary
            {
                Id = a.CandidateProfile!.Id,
                UserId = a.CandidateProfile.UserId,
                FirstName = a.CandidateProfile.FirstName,
                LastName = a.CandidateProfile.LastName,
                Email = a.CandidateProfile.Email,
                Headline = a.CandidateProfile.Headline,
                City = a.CandidateProfile.City,
                Situation = a.CandidateProfile.Situation,
                DesiredJob = a.CandidateProfile.DesiredJob,
                PhotoPath = a.CandidateProfile.PhotoPath,
                CreatedAt = a.CandidateProfile.CreatedAt,
                // Correspondance avec l'offre (candidatures reçues classées) et mise en avant premium
                DesiredDomainId = a.CandidateProfile.DesiredDomainId,
                DesiredProfessionId = a.CandidateProfile.DesiredProfessionId,
                DesiredProfessionName = a.CandidateProfile.DesiredProfession!.Name,
                Skills = a.CandidateProfile.Skills.Select(s => s.Name!).ToList(),
                ExperienceTitles = a.CandidateProfile.Experiences.Select(e => e.Title!).ToList(),
                WishProfessionIds = a.CandidateProfile.WishProfessionIds != null ? a.CandidateProfile.WishProfessionIds.ToList() : new List<int>(),
                AvailableFrom = a.CandidateProfile.AvailableFrom,
                PremiumGrantedUntil = a.CandidateProfile.PremiumGrantedUntil,
                PremiumPaidUntil = a.CandidateProfile.PremiumPaidUntil,
                IsTestAccount = a.CandidateProfile.IsTestAccount,
            },
        };
    }
}
