using HelpMuslimWebApp.Domain.Models;

namespace HelpMuslimWebApp.Domain.Services
{
    public interface ISiteStatsService
    {
        // Note une venue (identifiant tiré au hasard par le navigateur) ; false si déjà notée dans l'heure ou invalide
        Task<bool> RecordVisitAsync(string? visitorId, string? countryCode, string? source = null);

        // La période qui contient anchor (jour, semaine du lundi, mois, année), découpée et comptée en heure locale du site
        Task<SiteStats> GetStatsAsync(StatsScale scale, DateOnly anchor, TimeZoneInfo timeZone);

        // Abonnements (candidats et recruteurs) de la même période : mouvements, puis état à la fin de la période
        Task<SubscriptionStats> GetSubscriptionStatsAsync(StatsScale scale, DateOnly anchor, TimeZoneInfo timeZone);

        // Frais du coach IA sur la période : total, découpage dans le temps, et par candidat
        Task<CoachCostStats> GetCoachCostStatsAsync(StatsScale scale, DateOnly anchor, TimeZoneInfo timeZone);

        // Frais de l'assistant IA des recruteurs sur la période : total, par outil, dans le temps, et par recruteur
        Task<RecruiterAiCostStats> GetRecruiterAiCostStatsAsync(StatsScale scale, DateOnly anchor, TimeZoneInfo timeZone);

        // Gains des abonnements (mois ou année) aux prix actuels, et les douze mois de l'année
        Task<RevenueStats> GetRevenueStatsAsync(StatsScale scale, DateOnly anchor, TimeZoneInfo timeZone, PremiumSettings prices);
    }
}
