namespace HelpMuslimWebApp.Api.Request
{
    // Recruteur › Candidats : recherche libre et filtres
    public class CandidateMatchQuery
    {
        public string? Search { get; set; }

        // Une seule de ses offres ouvertes : candidats classés pour celle-ci
        public int? OfferId { get; set; }

        // Ville du candidat (casse et accents ignorés)
        public string? City { get; set; }

        // Disponibles d'ici N mois (0 : tout de suite) ; vide : toutes disponibilités, non renseignées comprises
        public int? AvailableWithinMonths { get; set; }

        public int? Take { get; set; }

        public bool IsFiltered => !string.IsNullOrWhiteSpace(Search) || OfferId != null || !string.IsNullOrWhiteSpace(City) || AvailableWithinMonths != null;
    }
}
