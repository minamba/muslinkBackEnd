using HelpMuslimWebApp.Domain.Models;

namespace HelpMuslimWebApp.Domain.Services
{
    public interface IJobOfferService
    {
        Task<PagedResult<JobOffer>> GetJobOffersAsync(OfferFilter filter);
        Task<IEnumerable<JobOffer>> GetFilteredJobOffersAsync(OfferFilter filter);
        // Statistiques : consultation de l'offre par un visiteur ou un candidat
        Task IncrementViewsAsync(int jobOfferId);
        Task<JobOffer?> GetJobOfferByIdAsync(int id);
        Task<JobOffer> AddJobOfferAsync(JobOffer model);
        Task<JobOffer?> UpdateJobOfferAsync(JobOffer model);
        // Fermer avec isFilled = true : le recruteur a trouvé son candidat
        Task<JobOffer?> SetJobOfferOpenAsync(int id, bool isOpen, bool isFilled = false);
        Task<FilledOffersStats> GetFilledOffersStatsAsync(int take);

        // Masquer / rendre visible une offre (le recruteur garde la main dessus)
        Task<JobOffer?> SetJobOfferHiddenAsync(int id, bool isHidden);

        // Le recruteur écrit à un candidat : ses offres ouvertes auxquelles ce candidat a postulé passent "En discussion"
        Task<int> MarkOffersInDiscussionAsync(int recruiterProfileId, int candidateProfileId);

        // Administration : retire du compteur les offres pourvues d'un recruteur (modération, données de démo)
        Task<int> DeleteOfferFillsAsync(int recruiterProfileId);

        // Offres favorites d'un candidat (les plus récentes en premier)
        Task<IEnumerable<JobOffer>> GetFavoriteOffersAsync(int candidateProfileId);
        Task<bool> OfferFavoriteExistsAsync(int candidateProfileId, int jobOfferId);
        Task AddOfferFavoriteAsync(int candidateProfileId, int jobOfferId);
        Task<bool> DeleteOfferFavoriteAsync(int candidateProfileId, int jobOfferId);
        Task<bool> DeleteJobOfferAsync(int id);
    }
}
