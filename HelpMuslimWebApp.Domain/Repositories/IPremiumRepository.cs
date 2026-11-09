using HelpMuslimWebApp.Domain.Models;

namespace HelpMuslimWebApp.Domain.Repositories
{
    public interface IPremiumRepository
    {
        Task<PremiumSettings> GetSettingsAsync();
        Task<PremiumSettings> UpdateSettingsAsync(PremiumSettings settings);

        // ---------- Candidats ----------
        // Candidats premium (offert ou payé, encore actif)
        Task<IEnumerable<PremiumMember>> GetMembersAsync();
        // Destinataires de l'email « selon vos vœux » : candidats premium actifs, ou tous (mode premium coupé)
        Task<IEnumerable<CandidateProfile>> GetWishAlertProfilesAsync(bool premiumOnly);
        Task<bool> SetGrantedUntilAsync(int candidateProfileId, DateTime? grantedUntil);
        Task SetStripeCustomerAsync(int candidateProfileId, string customerId);
        Task<int?> FindCandidateByStripeCustomerAsync(string customerId);
        // Événements notés au journal (SubscriptionEventKinds) : vide si rien n'a changé
        Task<IReadOnlyList<string>> ApplySubscriptionAsync(PremiumSubscriptionState state);

        // ---------- Recruteurs ----------
        Task<IEnumerable<ProMember>> GetProMembersAsync();
        // Abonnements payés en cours et renouvelés (résiliation non demandée) : recruteurs (Recruteur Pro) ou candidats (Premium)
        Task<List<PaidSubscription>> GetRenewingSubscriptionsAsync(bool recruiters);
        Task MarkCancelAtPeriodEndAsync(bool recruiters, int profileId);
        Task<bool> SetRecruiterGrantAsync(int recruiterProfileId, DateTime? grantedUntil, bool isFounder);
        // Recruteurs concernés par l'offre groupée : ceux qui ont publié au moins une offre (ou tous)
        Task<IEnumerable<RecruiterProfile>> GetRecruitersForGrantAsync(bool includeWithoutOffers);
        Task SetRecruiterStripeCustomerAsync(int recruiterProfileId, string customerId);
        Task<int?> FindRecruiterByStripeCustomerAsync(string customerId);
        Task<IReadOnlyList<string>> ApplyRecruiterSubscriptionAsync(PremiumSubscriptionState state);
        // Offres ouvertes hors bénévolat (limite du recruteur gratuit), en excluant éventuellement une offre
        Task<int> CountActiveOffersAsync(int recruiterProfileId, int? exceptOfferId = null);
        Task<DateTime?> SponsorOfferAsync(int jobOfferId, int days);
        Task MarkFreeSponsorUsedAsync(int recruiterProfileId);
    }
}
