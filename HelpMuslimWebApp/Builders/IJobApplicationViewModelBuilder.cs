using System.Security.Claims;
using HelpMuslimWebApp.Api.Request;
using HelpMuslimWebApp.Api.ViewModels;

namespace HelpMuslimWebApp.Api.Builders
{
    public interface IJobApplicationViewModelBuilder
    {
        Task<JobApplicationViewModel?> ApplyAsync(ClaimsPrincipal user, JobApplicationRequest model);  // null = déjà postulé
        Task<IEnumerable<JobApplicationViewModel>> GetMyApplicationsAsync(ClaimsPrincipal user);
        Task<IEnumerable<JobApplicationViewModel>?> GetApplicationsForOfferAsync(ClaimsPrincipal user, int jobOfferId);

        // Recruteur : toutes les candidatures reçues sur ses offres, avec la correspondance de chaque candidat à l'offre visée
        Task<IEnumerable<JobApplicationViewModel>> GetReceivedApplicationsAsync(ClaimsPrincipal user);
        Task<IEnumerable<JobApplicationViewModel>> GetAllApplicationsAsync();
        Task<JobApplicationDetailViewModel?> GetApplicationByIdAsync(ClaimsPrincipal user, int id);
        Task<bool> HideMyApplicationAsync(ClaimsPrincipal user, int id);     // candidat : offre fermée retirée de sa liste
        Task<bool> DeleteApplicationAsync(ClaimsPrincipal user, int id);

        // CV joint à la candidature (copie figée) : recruteur de l'offre, candidat auteur, administration
        Task<(Stream Stream, string FileName, string ContentType)?> GetApplicationCvAsync(ClaimsPrincipal user, int id);
    }
}
