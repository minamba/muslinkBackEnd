using HelpMuslimWebApp.Domain.Models;

namespace HelpMuslimWebApp.Domain.Repositories
{
    public interface IMatchAlertRepository
    {
        // Couples offre / candidat déjà signalés, et ceux où le candidat a déjà postulé (le recruteur le connaît)
        Task<HashSet<(int JobOfferId, int CandidateProfileId)>> GetAlertedPairsAsync();
        Task<HashSet<(int JobOfferId, int CandidateProfileId)>> GetApplicationPairsAsync();
        Task AddAlertsAsync(IReadOnlyCollection<MatchAlertPair> pairs);

        // Lien de désinscription de l'email : jeton propre au recruteur, créé au premier envoi
        Task<string> GetOrCreateTokenAsync(int recruiterProfileId);
        Task<bool> SetEnabledAsync(int recruiterProfileId, bool enabled);
        Task<bool> SetEnabledByTokenAsync(string token, bool enabled);
        Task<bool> SetMessageAlertsEnabledAsync(int recruiterProfileId, bool enabled);
        Task<bool> SetMessageAlertsEnabledByTokenAsync(string token, bool enabled);
        Task<bool> SetApplicationAlertsEnabledAsync(int recruiterProfileId, bool enabled);
        Task<bool> SetApplicationAlertsEnabledByTokenAsync(string token, bool enabled);
    }
}
