using HelpMuslimWebApp.Domain.Models;

namespace HelpMuslimWebApp.Domain.Services
{
    public interface IPremiumService
    {
        Task<PremiumSettings> GetSettingsAsync();
        Task<PremiumSettings> UpdateSettingsAsync(PremiumSettings settings);

        // ---------- Candidats ----------
        Task<IEnumerable<PremiumMember>> GetMembersAsync();
        // Destinataires de l'email « selon vos vœux » : abonnés premium (mode premium actif) ou tous les candidats (mode coupé)
        Task<IEnumerable<CandidateProfile>> GetWishAlertProfilesAsync(bool premiumOnly);
        // Offre le premium {months} mois (ajoutés à la période offerte en cours) ou à vie ; months = null et lifetime = false : retire
        Task<bool> GrantAsync(int candidateProfileId, DateTime? currentGrantedUntil, int? months, bool lifetime = false);
        Task SetStripeCustomerAsync(int candidateProfileId, string customerId);
        Task<int?> FindCandidateByStripeCustomerAsync(string customerId);
        // Événements notés au journal (SubscriptionEventKinds) : vide si rien n'a changé
        Task<IReadOnlyList<string>> ApplySubscriptionAsync(PremiumSubscriptionState state);

        // ---------- Recruteurs ----------
        Task<IEnumerable<ProMember>> GetProMembersAsync();
        // Abonnements payés en cours et renouvelés (résiliation non demandée) : recruteurs (Recruteur Pro) ou candidats (Premium)
        Task<List<PaidSubscription>> GetRenewingSubscriptionsAsync(bool recruiters);
        Task MarkCancelAtPeriodEndAsync(bool recruiters, int profileId);
        // À vie : entreprise fondatrice. months = null et lifetime = false : retire le Pro offert (et le statut fondateur)
        Task<bool> GrantRecruiterAsync(RecruiterProfile recruiter, int? months, bool lifetime);
        // Offre groupée : chaque recruteur garde sa période offerte si elle est plus longue ; retourne le nombre de recruteurs
        Task<int> GrantRecruitersAsync(int? months, bool lifetime, bool includeWithoutOffers);
        Task<int> CountRecruitersForGrantAsync(bool includeWithoutOffers);
        Task SetRecruiterStripeCustomerAsync(int recruiterProfileId, string customerId);
        Task<int?> FindRecruiterByStripeCustomerAsync(string customerId);
        Task<IReadOnlyList<string>> ApplyRecruiterSubscriptionAsync(PremiumSubscriptionState state);
        Task<int> CountActiveOffersAsync(int recruiterProfileId, int? exceptOfferId = null);
        Task<DateTime?> SponsorOfferAsync(int jobOfferId);
        Task MarkFreeSponsorUsedAsync(int recruiterProfileId);
    }
}
