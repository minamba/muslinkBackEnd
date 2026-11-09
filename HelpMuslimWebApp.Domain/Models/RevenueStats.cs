namespace HelpMuslimWebApp.Domain.Models
{
    // Tableau de bord › Gains : ce que représentent les abonnements payants (prix actuels de l'administration Premium).
    // Revenu mensuel : abonnements actifs × prix du mois ; Recruteur Pro annuel compté pour un douzième de son prix chaque mois.
    // Premium offerts : comptés à part, ils ne rapportent rien. Offres sponsorisées à l'unité : non suivies ici.
    public class RevenueStats
    {
        // Mois ou année
        public StatsScale Scale { get; set; }
        // Fenêtre en heure locale du site
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        // Début du journal des abonnements (heure locale) : rien n'est connu avant
        public DateTime? TrackedSince { get; set; }

        // Prix en centimes : candidats TTC, recruteurs HT
        public int CandidateMonthlyPriceCents { get; set; }
        public int RecruiterMonthlyPriceCents { get; set; }
        public int RecruiterYearlyPriceCents { get; set; }

        public AudienceRevenue Candidates { get; set; } = new();
        public AudienceRevenue Recruiters { get; set; } = new();
    }

    public class AudienceRevenue
    {
        // Gains de la période : le mois choisi, ou la somme des mois de l'année (jusqu'à aujourd'hui)
        public int PeriodCents { get; set; }

        // Fin de la période (ou maintenant pour la période en cours) : abonnés payants, dont formule mensuelle / annuelle,
        // résiliations programmées, Premium offerts, et ce que ces abonnés représentent par mois
        public int Paying { get; set; }
        public int Monthly { get; set; }
        public int Yearly { get; set; }
        public int Cancelling { get; set; }
        public int Offered { get; set; }
        public int MonthlyCents { get; set; }

        // Les douze mois de l'année de la période (null : mois à venir ou antérieur au suivi)
        public List<RevenuePoint> Year { get; set; } = [];
    }

    public class RevenuePoint
    {
        // Premier jour du mois, heure locale
        public DateTime Start { get; set; }
        public int? Paying { get; set; }
        public int? Cents { get; set; }
    }
}
