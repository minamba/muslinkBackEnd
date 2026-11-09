using HelpMuslimWebApp.Domain.Models;
using HelpMuslimWebApp.Domain.Repositories;

namespace HelpMuslimWebApp.Domain.Services.impl
{
    public class PremiumService : IPremiumService
    {
        private readonly IPremiumRepository _repository;

        public PremiumService(IPremiumRepository repository)
        {
            _repository = repository;
        }

        public Task<PremiumSettings> GetSettingsAsync() => _repository.GetSettingsAsync();

        public Task<PremiumSettings> UpdateSettingsAsync(PremiumSettings settings)
        {
            // Stripe : 0,50 € minimum par paiement
            foreach (var cents in new[] { settings.CandidateMonthlyPriceCents, settings.RecruiterMonthlyPriceCents, settings.RecruiterYearlyPriceCents, settings.SponsoredOfferPriceCents })
            {
                if (cents is < 50 or > 1_000_000) throw new ArgumentException("Chaque prix doit être compris entre 0,50 € et 10 000 €.");
            }
            return _repository.UpdateSettingsAsync(settings);
        }

        // ---------- Candidats ----------

        public Task<IEnumerable<PremiumMember>> GetMembersAsync() => _repository.GetMembersAsync();
        public Task<IEnumerable<CandidateProfile>> GetWishAlertProfilesAsync(bool premiumOnly) => _repository.GetWishAlertProfilesAsync(premiumOnly);

        public Task<bool> GrantAsync(int candidateProfileId, DateTime? currentGrantedUntil, int? months, bool lifetime = false) =>
            months == null && !lifetime
                ? _repository.SetGrantedUntilAsync(candidateProfileId, null)
                : _repository.SetGrantedUntilAsync(candidateProfileId, Premium.GrantUntil(currentGrantedUntil, months, lifetime));

        public Task SetStripeCustomerAsync(int candidateProfileId, string customerId) => _repository.SetStripeCustomerAsync(candidateProfileId, customerId);
        public Task<int?> FindCandidateByStripeCustomerAsync(string customerId) => _repository.FindCandidateByStripeCustomerAsync(customerId);
        public Task<IReadOnlyList<string>> ApplySubscriptionAsync(PremiumSubscriptionState state) => _repository.ApplySubscriptionAsync(state);

        // ---------- Recruteurs ----------

        public Task<IEnumerable<ProMember>> GetProMembersAsync() => _repository.GetProMembersAsync();
        public Task<List<PaidSubscription>> GetRenewingSubscriptionsAsync(bool recruiters) => _repository.GetRenewingSubscriptionsAsync(recruiters);
        public Task MarkCancelAtPeriodEndAsync(bool recruiters, int profileId) => _repository.MarkCancelAtPeriodEndAsync(recruiters, profileId);

        public Task<bool> GrantRecruiterAsync(RecruiterProfile recruiter, int? months, bool lifetime)
        {
            if (months == null && !lifetime) return _repository.SetRecruiterGrantAsync(recruiter.Id, null, false);
            var until = Premium.GrantUntil(recruiter.ProGrantedUntil, months, lifetime);
            return _repository.SetRecruiterGrantAsync(recruiter.Id, until, Premium.IsLifetime(until));
        }

        public async Task<int> GrantRecruitersAsync(int? months, bool lifetime, bool includeWithoutOffers)
        {
            if (!lifetime && (months == null || !Premium.GrantDurations.Contains(months.Value)))
                throw new ArgumentException($"Durée invalide : {string.Join(", ", Premium.GrantDurations)} mois, ou à vie.");

            var recruiters = (await _repository.GetRecruitersForGrantAsync(includeWithoutOffers)).ToList();
            var target = lifetime ? Premium.Lifetime : DateTime.UtcNow.AddMonths(months!.Value);
            foreach (var recruiter in recruiters)
            {
                // Une période offerte plus longue (ou à vie) est conservée
                var until = recruiter.ProGrantedUntil > target ? recruiter.ProGrantedUntil!.Value : target;
                await _repository.SetRecruiterGrantAsync(recruiter.Id, until, Premium.IsLifetime(until));
            }
            return recruiters.Count;
        }

        public async Task<int> CountRecruitersForGrantAsync(bool includeWithoutOffers) =>
            (await _repository.GetRecruitersForGrantAsync(includeWithoutOffers)).Count();

        public Task SetRecruiterStripeCustomerAsync(int recruiterProfileId, string customerId) => _repository.SetRecruiterStripeCustomerAsync(recruiterProfileId, customerId);
        public Task<int?> FindRecruiterByStripeCustomerAsync(string customerId) => _repository.FindRecruiterByStripeCustomerAsync(customerId);
        public Task<IReadOnlyList<string>> ApplyRecruiterSubscriptionAsync(PremiumSubscriptionState state) => _repository.ApplyRecruiterSubscriptionAsync(state);
        public Task<int> CountActiveOffersAsync(int recruiterProfileId, int? exceptOfferId = null) => _repository.CountActiveOffersAsync(recruiterProfileId, exceptOfferId);
        public Task<DateTime?> SponsorOfferAsync(int jobOfferId) => _repository.SponsorOfferAsync(jobOfferId, Premium.SponsorDays);
        public Task MarkFreeSponsorUsedAsync(int recruiterProfileId) => _repository.MarkFreeSponsorUsedAsync(recruiterProfileId);
    }
}
