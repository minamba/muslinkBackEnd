namespace HelpMuslimWebApp.Domain.Models
{
    // Alerte quotidienne aux recruteurs : candidat qui correspond à l'une de leurs offres à 70 % au moins
    public static class MatchAlerts
    {
        public const int Threshold = 70;
        // Candidats détaillés dans l'email (les suivants sont résumés par « et N autres »)
        public const int MaxListed = 10;
    }

    public record MatchAlertPair(int JobOfferId, int CandidateProfileId, int Score);
}
