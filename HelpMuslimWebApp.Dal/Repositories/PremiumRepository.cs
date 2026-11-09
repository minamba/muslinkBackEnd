using HelpMuslimWebApp.Dal.Entities;
using HelpMuslimWebApp.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Models = HelpMuslimWebApp.Domain.Models;

namespace HelpMuslimWebApp.Dal.Repositories
{
    public class PremiumRepository : IPremiumRepository
    {
        private const int SettingsId = 1;
        private readonly HelpMuslimWebAppDatabaseContext _context;

        public PremiumRepository(HelpMuslimWebAppDatabaseContext context)
        {
            _context = context;
        }

        public async Task<Models.PremiumSettings> GetSettingsAsync()
        {
            var settings = await _context.PremiumSettings.AsNoTracking().FirstOrDefaultAsync(s => s.Id == SettingsId);
            return settings == null
                ? new Models.PremiumSettings { CandidateMonthlyPriceCents = 699, RecruiterMonthlyPriceCents = 4900, RecruiterYearlyPriceCents = 49000, SponsoredOfferPriceCents = 2900 }
                : ToModel(settings);
        }

        public async Task<Models.PremiumSettings> UpdateSettingsAsync(Models.PremiumSettings model)
        {
            var settings = await _context.PremiumSettings.FirstOrDefaultAsync(s => s.Id == SettingsId);
            if (settings == null)
            {
                settings = new PremiumSetting { Id = SettingsId };
                _context.PremiumSettings.Add(settings);
            }
            settings.CandidatesEnabled = model.CandidatesEnabled;
            settings.CandidateMonthlyPriceCents = model.CandidateMonthlyPriceCents;
            settings.RecruitersEnabled = model.RecruitersEnabled;
            settings.RecruiterMonthlyPriceCents = model.RecruiterMonthlyPriceCents;
            settings.RecruiterYearlyPriceCents = model.RecruiterYearlyPriceCents;
            settings.SponsoredOfferPriceCents = model.SponsoredOfferPriceCents;
            settings.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return ToModel(settings);
        }

        public async Task<IEnumerable<Models.PremiumMember>> GetMembersAsync()
        {
            var now = DateTime.UtcNow;
            return await _context.CandidateProfiles.AsNoTracking()
                .Where(c => c.PremiumGrantedUntil > now || c.PremiumPaidUntil > now)
                .OrderBy(c => c.LastName).ThenBy(c => c.FirstName)
                .Select(c => new Models.PremiumMember
                {
                    CandidateProfileId = c.Id,
                    FirstName = c.FirstName,
                    LastName = c.LastName,
                    Email = c.Email,
                    GrantedUntil = c.PremiumGrantedUntil,
                    PaidUntil = c.PremiumPaidUntil,
                    CancelAtPeriodEnd = c.PremiumCancelAtPeriodEnd == true,
                    HasSubscription = c.StripeSubscriptionId != null,
                })
                .ToListAsync();
        }

        public async Task<IEnumerable<Models.CandidateProfile>> GetWishAlertProfilesAsync(bool premiumOnly)
        {
            var now = DateTime.UtcNow;
            var query = _context.CandidateProfiles.AsNoTracking()
                .Include(c => c.DesiredDomain).Include(c => c.DesiredProfession)
                .Where(c => c.Email != null && c.Email != "");
            if (premiumOnly) query = query.Where(c => c.PremiumGrantedUntil > now || c.PremiumPaidUntil > now);
            var entities = await query.ToListAsync();
            return entities.Select(CandidateProfileRepository.ToModel).ToList();
        }

        public async Task<bool> SetGrantedUntilAsync(int candidateProfileId, DateTime? grantedUntil)
        {
            var previous = await _context.CandidateProfiles.AsNoTracking().Where(c => c.Id == candidateProfileId)
                .Select(c => new { c.PremiumGrantedUntil }).FirstOrDefaultAsync();
            if (previous == null) return false;
            await _context.CandidateProfiles.Where(c => c.Id == candidateProfileId)
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.PremiumGrantedUntil, grantedUntil));
            await LogGrantAsync(Models.SubscriptionAudiences.Candidate, candidateProfileId, previous.PremiumGrantedUntil, grantedUntil);
            return true;
        }

        public Task SetStripeCustomerAsync(int candidateProfileId, string customerId) =>
            _context.CandidateProfiles.Where(c => c.Id == candidateProfileId)
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.StripeCustomerId, customerId));

        public async Task<int?> FindCandidateByStripeCustomerAsync(string customerId) =>
            await _context.CandidateProfiles.AsNoTracking()
                .Where(c => c.StripeCustomerId == customerId)
                .Select(c => (int?)c.Id)
                .FirstOrDefaultAsync();

        public async Task<IReadOnlyList<string>> ApplySubscriptionAsync(Models.PremiumSubscriptionState state)
        {
            var candidate = await _context.CandidateProfiles.FirstOrDefaultAsync(c => c.Id == state.ProfileId);
            if (candidate == null) return [];

            if (state.CustomerId != null) candidate.StripeCustomerId = state.CustomerId;
            // Abonnement terminé : on n'efface que s'il s'agit bien de l'abonnement en cours (un ancien abonnement
            // annulé ne coupe pas un nouvel abonnement)
            if (state.PaidUntil == null && candidate.StripeSubscriptionId != null && candidate.StripeSubscriptionId != state.SubscriptionId) return [];
            var events = LogSubscriptionChange(Models.SubscriptionAudiences.Candidate, state, candidate.StripeSubscriptionId, candidate.PremiumPaidUntil, candidate.PremiumCancelAtPeriodEnd == true);
            if (state.PaidUntil == null)
            {
                candidate.StripeSubscriptionId = null;
                candidate.PremiumPaidUntil = DateTime.UtcNow;
                candidate.PremiumCancelAtPeriodEnd = false;
            }
            else
            {
                candidate.StripeSubscriptionId = state.SubscriptionId;
                candidate.PremiumPaidUntil = state.PaidUntil;
                candidate.PremiumCancelAtPeriodEnd = state.CancelAtPeriodEnd;
            }
            await _context.SaveChangesAsync();
            return events;
        }

        // ---------- Recruteurs ----------

        public async Task<IEnumerable<Models.ProMember>> GetProMembersAsync()
        {
            var now = DateTime.UtcNow;
            return await _context.RecruiterProfiles.AsNoTracking()
                .Where(r => r.ProGrantedUntil > now || r.ProPaidUntil > now)
                .OrderByDescending(r => r.IsFounder).ThenBy(r => r.CompanyName)
                .Select(r => new Models.ProMember
                {
                    RecruiterProfileId = r.Id,
                    CompanyName = r.CompanyName,
                    FirstName = r.FirstName,
                    LastName = r.LastName,
                    Email = r.Email,
                    GrantedUntil = r.ProGrantedUntil,
                    PaidUntil = r.ProPaidUntil,
                    IsFounder = r.IsFounder == true,
                    CancelAtPeriodEnd = r.ProCancelAtPeriodEnd == true,
                    HasSubscription = r.StripeSubscriptionId != null,
                })
                .ToListAsync();
        }

        public async Task<List<Models.PaidSubscription>> GetRenewingSubscriptionsAsync(bool recruiters)
        {
            var now = DateTime.UtcNow;
            return recruiters
                ? await _context.RecruiterProfiles.AsNoTracking()
                    .Where(r => r.StripeSubscriptionId != null && r.ProPaidUntil > now && r.ProCancelAtPeriodEnd != true)
                    .Select(r => new Models.PaidSubscription { ProfileId = r.Id, Email = r.Email, FirstName = r.FirstName, SubscriptionId = r.StripeSubscriptionId!, PaidUntil = r.ProPaidUntil })
                    .ToListAsync()
                : await _context.CandidateProfiles.AsNoTracking()
                    .Where(c => c.StripeSubscriptionId != null && c.PremiumPaidUntil > now && c.PremiumCancelAtPeriodEnd != true)
                    .Select(c => new Models.PaidSubscription { ProfileId = c.Id, Email = c.Email, FirstName = c.FirstName, SubscriptionId = c.StripeSubscriptionId!, PaidUntil = c.PremiumPaidUntil })
                    .ToListAsync();
        }

        public async Task MarkCancelAtPeriodEndAsync(bool recruiters, int profileId)
        {
            var updated = recruiters
                ? await _context.RecruiterProfiles.Where(r => r.Id == profileId && r.ProCancelAtPeriodEnd != true)
                    .ExecuteUpdateAsync(s => s.SetProperty(r => r.ProCancelAtPeriodEnd, true))
                : await _context.CandidateProfiles.Where(c => c.Id == profileId && c.PremiumCancelAtPeriodEnd != true)
                    .ExecuteUpdateAsync(s => s.SetProperty(c => c.PremiumCancelAtPeriodEnd, true));
            if (updated == 0) return;
            Log(recruiters ? Models.SubscriptionAudiences.Recruiter : Models.SubscriptionAudiences.Candidate, profileId, Models.SubscriptionEventKinds.CancelRequested);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> SetRecruiterGrantAsync(int recruiterProfileId, DateTime? grantedUntil, bool isFounder)
        {
            var query = _context.RecruiterProfiles.Where(r => r.Id == recruiterProfileId);
            var previous = await query.AsNoTracking().Select(r => new { r.ProGrantedUntil }).FirstOrDefaultAsync();
            if (previous == null) return false;
            var now = DateTime.UtcNow;
            // Fondatrice depuis : gardé si elle l'était déjà, effacé quand le statut est retiré
            if (isFounder)
                await query.ExecuteUpdateAsync(s => s.SetProperty(r => r.ProGrantedUntil, grantedUntil).SetProperty(r => r.IsFounder, true)
                    .SetProperty(r => r.FounderSince, r => r.FounderSince ?? now));
            else
                await query.ExecuteUpdateAsync(s => s.SetProperty(r => r.ProGrantedUntil, grantedUntil).SetProperty(r => r.IsFounder, false)
                    .SetProperty(r => r.FounderSince, (DateTime?)null));
            await LogGrantAsync(Models.SubscriptionAudiences.Recruiter, recruiterProfileId, previous.ProGrantedUntil, grantedUntil);
            return true;
        }

        public async Task<IEnumerable<Models.RecruiterProfile>> GetRecruitersForGrantAsync(bool includeWithoutOffers) =>
            await _context.RecruiterProfiles.AsNoTracking()
                .Where(r => r.IsDisabled != true && (includeWithoutOffers || r.Offers.Any()))
                .Select(r => new Models.RecruiterProfile { Id = r.Id, CompanyName = r.CompanyName, ProGrantedUntil = r.ProGrantedUntil, IsFounder = r.IsFounder == true })
                .ToListAsync();

        public Task SetRecruiterStripeCustomerAsync(int recruiterProfileId, string customerId) =>
            _context.RecruiterProfiles.Where(r => r.Id == recruiterProfileId)
                .ExecuteUpdateAsync(s => s.SetProperty(r => r.StripeCustomerId, customerId));

        public async Task<int?> FindRecruiterByStripeCustomerAsync(string customerId) =>
            await _context.RecruiterProfiles.AsNoTracking()
                .Where(r => r.StripeCustomerId == customerId)
                .Select(r => (int?)r.Id)
                .FirstOrDefaultAsync();

        public async Task<IReadOnlyList<string>> ApplyRecruiterSubscriptionAsync(Models.PremiumSubscriptionState state)
        {
            var recruiter = await _context.RecruiterProfiles.FirstOrDefaultAsync(r => r.Id == state.ProfileId);
            if (recruiter == null) return [];

            if (state.CustomerId != null) recruiter.StripeCustomerId = state.CustomerId;
            // Abonnement terminé : seulement s'il s'agit de l'abonnement en cours
            if (state.PaidUntil == null && recruiter.StripeSubscriptionId != null && recruiter.StripeSubscriptionId != state.SubscriptionId) return [];
            var events = LogSubscriptionChange(Models.SubscriptionAudiences.Recruiter, state, recruiter.StripeSubscriptionId, recruiter.ProPaidUntil, recruiter.ProCancelAtPeriodEnd == true);
            if (state.PaidUntil == null)
            {
                recruiter.StripeSubscriptionId = null;
                recruiter.ProPaidUntil = DateTime.UtcNow;
                recruiter.ProCancelAtPeriodEnd = false;
            }
            else
            {
                recruiter.StripeSubscriptionId = state.SubscriptionId;
                recruiter.ProPaidUntil = state.PaidUntil;
                recruiter.ProCancelAtPeriodEnd = state.CancelAtPeriodEnd;
            }
            await _context.SaveChangesAsync();
            return events;
        }

        public Task<int> CountActiveOffersAsync(int recruiterProfileId, int? exceptOfferId = null) =>
            _context.JobOffers.AsNoTracking()
                .CountAsync(o => o.RecruiterProfileId == recruiterProfileId && o.IsOpen == true
                    && o.ContractType != Models.ContractTypes.Benevolat && (exceptOfferId == null || o.Id != exceptOfferId));

        // Prolonge un parrainage en cours, sinon part d'aujourd'hui
        public async Task<DateTime?> SponsorOfferAsync(int jobOfferId, int days)
        {
            var offer = await _context.JobOffers.FirstOrDefaultAsync(o => o.Id == jobOfferId);
            if (offer == null) return null;
            var start = offer.SponsoredUntil > DateTime.UtcNow ? offer.SponsoredUntil.Value : DateTime.UtcNow;
            offer.SponsoredUntil = start.AddDays(days);
            await _context.SaveChangesAsync();
            return offer.SponsoredUntil;
        }

        public Task MarkFreeSponsorUsedAsync(int recruiterProfileId) =>
            _context.RecruiterProfiles.Where(r => r.Id == recruiterProfileId)
                .ExecuteUpdateAsync(s => s.SetProperty(r => r.LastFreeSponsorAt, DateTime.UtcNow));

        // ---------- Journal des abonnements (Tableau de bord › Abonnements)

        private void Log(string audience, int profileId, string kind, string? subscriptionId = null, string? plan = null, DateTime? grantedUntil = null) =>
            _context.SubscriptionEvents.Add(new SubscriptionEvent
            {
                Audience = audience, ProfileId = profileId, Kind = kind, SubscriptionId = subscriptionId, Plan = plan,
                GrantedUntil = grantedUntil, OccurredAt = DateTime.UtcNow,
            });

        // Événements d'après l'état avant / après : Stripe renvoie parfois deux fois le même état, qui ne note rien de plus.
        // Retourne les événements notés
        private List<string> LogSubscriptionChange(string audience, Models.PremiumSubscriptionState state, string? previousId, DateTime? previousPaidUntil, bool previousCancel)
        {
            var events = new List<string>();
            void Note(string kind, string? subscriptionId, string? plan = null)
            {
                Log(audience, state.ProfileId, kind, subscriptionId, plan);
                events.Add(kind);
            }

            if (state.PaidUntil == null)
            {
                if (previousId != null) Note(Models.SubscriptionEventKinds.Ended, previousId);
                return events;
            }
            var same = previousId == state.SubscriptionId;
            // Un nouvel abonnement qui en remplace un autre encore actif n'est pas un nouvel abonné
            var wasActive = previousId != null && previousPaidUntil > DateTime.UtcNow;
            if (!same && !wasActive) Note(Models.SubscriptionEventKinds.Subscribed, state.SubscriptionId, state.Plan);
            var wasCancelling = same && previousCancel;
            if (state.CancelAtPeriodEnd && !wasCancelling) Note(Models.SubscriptionEventKinds.CancelRequested, state.SubscriptionId);
            else if (!state.CancelAtPeriodEnd && wasCancelling) Note(Models.SubscriptionEventKinds.CancelReverted, state.SubscriptionId);
            return events;
        }

        private async Task LogGrantAsync(string audience, int profileId, DateTime? previous, DateTime? grantedUntil)
        {
            if (previous == grantedUntil) return;
            var active = Models.Premium.IsActive(grantedUntil);
            // Retrait d'un premium déjà expiré : rien à noter
            if (!active && !Models.Premium.IsActive(previous)) return;
            Log(audience, profileId, active ? Models.SubscriptionEventKinds.Granted : Models.SubscriptionEventKinds.GrantRemoved, grantedUntil: active ? grantedUntil : null);
            await _context.SaveChangesAsync();
        }

        private static Models.PremiumSettings ToModel(PremiumSetting s) => new()
        {
            CandidatesEnabled = s.CandidatesEnabled,
            CandidateMonthlyPriceCents = s.CandidateMonthlyPriceCents,
            RecruitersEnabled = s.RecruitersEnabled,
            RecruiterMonthlyPriceCents = s.RecruiterMonthlyPriceCents,
            RecruiterYearlyPriceCents = s.RecruiterYearlyPriceCents,
            SponsoredOfferPriceCents = s.SponsoredOfferPriceCents,
            LaunchOfferEnabled = s.LaunchOfferEnabled,
            FounderSlots = s.FounderSlots,
            FounderAutoGrant = s.FounderAutoGrant,
            UpdatedAt = s.UpdatedAt,
        };
    }
}
