using HelpMuslimWebApp.Domain.Models;

namespace HelpMuslimWebApp.Domain.Repositories
{
    public interface ICvCoachRepository
    {
        // Dernière analyse du candidat (byAdmin = false) ou du super admin pour ce candidat (true)
        Task<CvCoachAnalysis?> GetLatestAsync(int candidateProfileId, bool byAdmin);
        Task<CvCoachAnalysis> AddAsync(CvCoachAnalysis model);
        // Le candidat supprime ses analyses : résultat effacé, date et tokens gardés (quota, frais) ; retourne le nombre d'analyses supprimées
        Task<int> DeleteCandidateAnalysesAsync(int candidateProfileId);
        // Analyses lancées par le candidat depuis cette date (UTC) : quota du mois
        Task<int> CountSinceAsync(int candidateProfileId, DateTime sinceUtc);
    }
}
