using HelpMuslimWebApp.Domain.Models;

namespace HelpMuslimWebApp.Domain.Repositories
{
    public interface ISiteStatsRepository
    {
        // Note une venue ; ignorée (false) si ce visiteur est déjà venu dans l'heure
        Task<bool> RecordVisitAsync(string visitorId, string? countryCode, string? source, DateTime nowUtc);

        Task<List<SiteVisitRow>> GetVisitsAsync(DateTime fromUtc, DateTime toUtc);

        // Dates de création (UTC) des profils et des candidatures sur la fenêtre : le tunnel de conversion
        Task<List<DateTime>> GetCandidateSignupsAsync(DateTime fromUtc, DateTime toUtc);
        Task<List<DateTime>> GetRecruiterSignupsAsync(DateTime fromUtc, DateTime toUtc);
        Task<List<DateTime>> GetApplicationsAsync(DateTime fromUtc, DateTime toUtc);

        // Recrutement (comptes de test exclus) : offres publiées sur la fenêtre, recruteurs ayant publié ou écrit, candidats ayant postulé
        Task<List<PublishedOfferRow>> GetPublishedOffersAsync(DateTime fromUtc, DateTime toUtc);
        Task<int> CountActiveRecruitersAsync(DateTime fromUtc, DateTime toUtc);
        Task<int> CountActiveCandidatesAsync(DateTime fromUtc, DateTime toUtc);

        // Offres pourvues grâce à la plateforme sur la fenêtre (FilledAt en UTC)
        Task<List<FilledOfferRow>> GetFilledOffersAsync(DateTime fromUtc, DateTime toUtc);

        // Abonnements : journal jusqu'à toUtc (dans l'ordre), profils existants et début du journal
        Task<List<SubscriptionEventRow>> GetSubscriptionEventsAsync(string audience, DateTime toUtc);
        Task<List<SubscriptionProfileRow>> GetSubscriptionProfilesAsync(string audience);
        Task<DateTime?> GetSubscriptionTrackedSinceAsync();
        // Analyses du coach IA de la période (tous candidats, y compris celles du super admin)
        Task<List<CoachUsageRow>> GetCoachUsageAsync(DateTime fromUtc, DateTime toUtc);
        // Appels à l'assistant IA des recruteurs de la période
        Task<List<RecruiterAiUsageRow>> GetRecruiterAiUsageAsync(DateTime fromUtc, DateTime toUtc);
    }
}
