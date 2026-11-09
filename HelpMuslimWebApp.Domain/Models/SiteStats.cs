namespace HelpMuslimWebApp.Domain.Models
{
    // Une venue (lecture pour les statistiques)
    public record SiteVisitRow(string VisitorId, string? CountryCode, DateTime CreatedAt, string? Source = null);

    // Provenance des visiteurs (voir VisitSources)
    public record SourceCount(string? Source, int Visitors);

    // Offre publiée sur la période : date de publication, première candidature reçue (UTC) et nombre de candidatures
    public record PublishedOfferRow(DateTime CreatedAt, DateTime? FirstApplicationAt, int Applications);

    public static class VisitSources
    {
        public static readonly IReadOnlyList<string> All = ["google", "moteur", "reseaux", "agregateur", "site", "direct"];
    }

    // Recrutement sur la période (comptes de test exclus)
    public class RecruitmentStats
    {
        public int PublishedOffers { get; set; }
        // Candidatures reçues par les offres publiées sur la période (jusqu'à aujourd'hui), en moyenne
        public double? ApplicationsPerOffer { get; set; }
        // Délai médian entre la publication et la première candidature, en heures
        public double? MedianHoursToFirstApplication { get; set; }
        // Offres publiées depuis au moins 7 jours, et celles d'entre elles qui n'avaient aucune candidature à 7 jours
        public int OffersAfter7Days { get; set; }
        public int OffersWithoutApplicationAfter7Days { get; set; }
        // Recruteurs qui ont publié une offre ou écrit à un candidat ; candidats qui ont postulé
        public int ActiveRecruiters { get; set; }
        public int ActiveCandidates { get; set; }
    }

    // Échelle de l'onglet Fréquentation : elle fixe la fenêtre et son découpage
    // (un jour en heures, une semaine et un mois en jours, une année en mois)
    public enum StatsScale
    {
        Day,
        Week,
        Month,
        Year,
    }

    public class StatsPoint
    {
        // Début du découpage, en heure locale du site (sans fuseau)
        public DateTime Start { get; set; }
        public int Visitors { get; set; }
        public int Candidates { get; set; }
        public int Recruiters { get; set; }
        public int Applications { get; set; }
        public int Filled { get; set; }
    }

    public record CountryCount(string? Code, int Visitors);

    // Offre pourvue grâce à la plateforme (recopiée : elle survit à la suppression de l'offre)
    public record FilledOfferRow(int? OfferId, string? Title, string? CompanyName, string? City, string? ContractType, DateTime FilledAt);

    public record ContractCount(string? ContractType, int Count);

    public class SiteStats
    {
        public StatsScale Scale { get; set; }
        public DateTime From { get; set; }
        public DateTime To { get; set; }

        // Visiteurs distincts sur toute la période (pas la somme des points : un visiteur venu deux jours compte une fois)
        public int Visitors { get; set; }
        public int Signups { get; set; }
        public int Candidates { get; set; }
        public int Recruiters { get; set; }
        public int Applications { get; set; }
        public int Filled { get; set; }
        public List<StatsPoint> Points { get; set; } = [];
        public List<CountryCount> Countries { get; set; } = [];
        public List<SourceCount> Sources { get; set; } = [];
        public RecruitmentStats Recruitment { get; set; } = new();

        // Offres pourvues : par type de contrat, et les plus récentes de la période (FilledAt en heure locale)
        public List<ContractCount> FilledByContract { get; set; } = [];
        public List<FilledOfferRow> LatestFilled { get; set; } = [];
    }
}
