using HelpMuslimWebApp.Api.Builders;
using HelpMuslimWebApp.Api.Request;
using HelpMuslimWebApp.Api.Utils;
using HelpMuslimWebApp.Api.ViewModels;
using HelpMuslimWebApp.Domain.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace HelpMuslimWebApp.Api.Controllers
{
    [ApiController]
    [Route("applications")]
    [Authorize]
    public class JobApplicationController : Controller
    {
        private readonly IJobApplicationViewModelBuilder _builder;

        public JobApplicationController(IJobApplicationViewModelBuilder builder)
        {
            _builder = builder ?? throw new ArgumentNullException(nameof(builder));
        }

        // "Je postule" : lettre de motivation obligatoire
        [HttpPost]
        [Authorize(Policy = AuthorizationPolicies.Candidat)]
        [SwaggerResponse(StatusCodes.Status200OK, "Candidature envoyée", typeof(JobApplicationViewModel))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Lettre de motivation manquante ou trop courte")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Offre introuvable")]
        [SwaggerResponse(StatusCodes.Status409Conflict, "Déjà postulé ou offre fermée")]
        public async Task<IActionResult> Apply([FromBody] JobApplicationRequest model)
        {
            var result = await _builder.ApplyAsync(User, model);
            if (result == null) return Conflict(new { message = "Vous avez déjà postulé à cette offre." });
            return Ok(result);
        }

        [HttpGet("mine")]
        [Authorize(Policy = AuthorizationPolicies.Candidat)]
        [SwaggerResponse(StatusCodes.Status200OK, "Mes candidatures", typeof(IEnumerable<JobApplicationViewModel>))]
        public async Task<IActionResult> GetMyApplications() => Ok(await _builder.GetMyApplicationsAsync(User));

        // Recruteur : toutes les candidatures reçues sur ses offres (ouvertes et fermées), avec leur correspondance
        [HttpGet("received")]
        [Authorize(Policy = AuthorizationPolicies.Recruteur)]
        [SwaggerResponse(StatusCodes.Status200OK, "Candidatures reçues, de la plus proche à la plus éloignée de l'offre visée", typeof(IEnumerable<JobApplicationViewModel>))]
        public async Task<IActionResult> GetReceivedApplications() => Ok(await _builder.GetReceivedApplicationsAsync(User));

        [HttpGet("offer/{offerId:int}")]
        [SwaggerResponse(StatusCodes.Status200OK, "Candidatures reçues pour une offre", typeof(IEnumerable<JobApplicationViewModel>))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "Pas propriétaire de l'offre")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Offre introuvable")]
        public async Task<IActionResult> GetApplicationsForOffer(int offerId)
        {
            var result = await _builder.GetApplicationsForOfferAsync(User, offerId);
            return result == null ? NotFound() : Ok(result);
        }

        [HttpGet]
        [Authorize(Policy = Permissions.Applications.View)]
        [SwaggerResponse(StatusCodes.Status200OK, "Toutes les candidatures", typeof(IEnumerable<JobApplicationViewModel>))]
        public async Task<IActionResult> GetApplications() => Ok(await _builder.GetAllApplicationsAsync());

        // Fiche candidature (lien de l'email recruteur) : lettre + offre + profil complet
        [HttpGet("{id:int}")]
        [SwaggerResponse(StatusCodes.Status200OK, "Fiche candidature", typeof(JobApplicationDetailViewModel))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "Accès refusé")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Candidature introuvable")]
        public async Task<IActionResult> GetApplication(int id)
        {
            var result = await _builder.GetApplicationByIdAsync(User, id);
            return result == null ? NotFound() : Ok(result);
        }

        [HttpGet("{id:int}/cv")]
        [SwaggerResponse(StatusCodes.Status200OK, "CV joint à la candidature")]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "Accès refusé")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Aucun CV joint")]
        public async Task<IActionResult> DownloadApplicationCv(int id)
        {
            var cv = await _builder.GetApplicationCvAsync(User, id);
            return cv == null ? NotFound() : File(cv.Value.Stream, cv.Value.ContentType, cv.Value.FileName);
        }

        // Candidat : retirer de « Mes candidatures » une candidature dont l'offre est fermée (sinon elle disparaît au bout d'une semaine)
        [HttpPatch("{id:int}/hide")]
        [Authorize(Policy = AuthorizationPolicies.Candidat)]
        [SwaggerResponse(StatusCodes.Status200OK, "Candidature retirée de la liste du candidat (le recruteur la conserve)")]
        [SwaggerResponse(StatusCodes.Status409Conflict, "Offre encore ouverte")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Candidature introuvable")]
        public async Task<IActionResult> HideApplication(int id) => await _builder.HideMyApplicationAsync(User, id) ? Ok() : NotFound();

        [HttpDelete("{id:int}")]
        [SwaggerResponse(StatusCodes.Status200OK, "Candidature retirée / supprimée")]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "Accès refusé")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Candidature introuvable")]
        public async Task<IActionResult> DeleteApplication(int id) => await _builder.DeleteApplicationAsync(User, id) ? Ok() : NotFound();
    }
}
