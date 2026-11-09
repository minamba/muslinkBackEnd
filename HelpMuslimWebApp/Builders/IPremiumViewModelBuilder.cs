using System.Security.Claims;
using HelpMuslimWebApp.Api.Request;
using HelpMuslimWebApp.Api.ViewModels;

namespace HelpMuslimWebApp.Api.Builders
{
    public interface IPremiumViewModelBuilder
    {
        Task<PremiumInfoViewModel> GetInfoAsync(ClaimsPrincipal user);
        Task<RedirectUrlViewModel> CreateCheckoutAsync(ClaimsPrincipal user);
        Task<RedirectUrlViewModel> CreateRecruiterCheckoutAsync(ClaimsPrincipal user, string interval);
        // Espace client Stripe du compte connecté (candidat ou recruteur)
        Task<RedirectUrlViewModel> CreatePortalAsync(ClaimsPrincipal user);
        Task<SponsorResultViewModel> SponsorOfferAsync(ClaimsPrincipal user, int jobOfferId);
        Task HandleWebhookAsync(string json, string? signature);

        Task<PremiumAdminViewModel> GetAdminAsync();
        Task<PremiumAdminViewModel> UpdateSettingsAsync(PremiumSettingsRequest model);
        // Crée / met à jour les produits et prix dans Stripe
        Task<PremiumAdminViewModel> SyncStripeAsync();
        Task<bool> GrantAsync(int candidateProfileId, PremiumGrantRequest model);
        Task<bool> RevokeGrantAsync(int candidateProfileId);
        Task<bool> GrantRecruiterAsync(int recruiterProfileId, PremiumGrantRequest model);
        Task<bool> RevokeRecruiterGrantAsync(int recruiterProfileId);
        // Abonnement payé (Stripe) : plus de renouvellement, avantages gardés jusqu'à la fin de la période payée ; email à l'abonné
        Task<bool> StopSubscriptionAsync(bool recruiter, int profileId);
        Task<RecruiterGrantPreviewViewModel> PreviewRecruitersGrantAsync(bool includeWithoutOffers);
        Task<RecruiterGrantPreviewViewModel> GrantRecruitersAsync(RecruiterBulkGrantRequest model);
    }
}
