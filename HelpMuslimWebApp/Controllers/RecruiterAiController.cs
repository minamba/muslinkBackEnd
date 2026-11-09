using HelpMuslimWebApp.Api.Builders;
using HelpMuslimWebApp.Api.Request;
using HelpMuslimWebApp.Api.Utils;
using HelpMuslimWebApp.Api.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace HelpMuslimWebApp.Api.Controllers
{
    // Assistant IA du recruteur (Recruteur Pro) : rédaction et relecture d'offres, brouillons de messages aux candidats
    [ApiController]
    [Route("recruiters/me/ai")]
    [Authorize(Policy = AuthorizationPolicies.Recruteur)]
    public class RecruiterAiController : ControllerBase
    {
        private readonly IRecruiterAiViewModelBuilder _builder;

        public RecruiterAiController(IRecruiterAiViewModelBuilder builder)
        {
            _builder = builder;
        }

        [HttpGet]
        [SwaggerResponse(StatusCodes.Status200OK, "État de l'assistant et quotas du mois", typeof(RecruiterAiViewModel))]
        public async Task<IActionResult> Get() => Ok(await _builder.GetAsync(User));

        // Offre pré-remplie à partir de notes et/ou d'une fiche de poste (multipart : notes, file)
        [HttpPost("offer-draft")]
        [RequestSizeLimit(12 * 1024 * 1024)]
        [SwaggerResponse(StatusCodes.Status200OK, "Champs de l'offre à relire", typeof(OfferDraftViewModel))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Notes trop courtes ou fichier illisible")]
        [SwaggerResponse(StatusCodes.Status409Conflict, "Pro requis, démo, assistant indisponible ou quota du mois atteint")]
        public async Task<IActionResult> DraftOffer([FromForm] OfferDraftRequest model, CancellationToken cancellationToken) =>
            Ok(await _builder.DraftOfferAsync(User, model, cancellationToken));

        // Relecture de l'offre du formulaire avant publication
        [HttpPost("offer-review")]
        [SwaggerResponse(StatusCodes.Status200OK, "Remarques sur l'offre", typeof(OfferReviewViewModel))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Offre encore vide")]
        [SwaggerResponse(StatusCodes.Status409Conflict, "Pro requis, démo, assistant indisponible ou quota du mois atteint")]
        public async Task<IActionResult> ReviewOffer([FromBody] OfferReviewRequest model, CancellationToken cancellationToken) =>
            Ok(await _builder.ReviewOfferAsync(User, model, cancellationToken));

        // Analyse d'une offre du recruteur : chiffres de Muslink, diagnostic et conseils
        [HttpPost("offers/{offerId:int}/insights")]
        [SwaggerResponse(StatusCodes.Status200OK, "Chiffres et conseils", typeof(OfferInsightsViewModel))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Offre introuvable")]
        [SwaggerResponse(StatusCodes.Status409Conflict, "Pro requis, démo, assistant indisponible ou quota du mois atteint")]
        public async Task<IActionResult> AnalyzeOffer(int offerId, CancellationToken cancellationToken) =>
            await _builder.AnalyzeOfferAsync(User, offerId, cancellationToken) is { } insights ? Ok(insights) : NotFound();

        // Questions d'entretien pour une offre du recruteur
        [HttpPost("offers/{offerId:int}/interview-questions")]
        [SwaggerResponse(StatusCodes.Status200OK, "Questions par thème", typeof(InterviewQuestionsViewModel))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Offre introuvable")]
        [SwaggerResponse(StatusCodes.Status409Conflict, "Pro requis, démo, assistant indisponible ou quota du mois atteint")]
        public async Task<IActionResult> InterviewQuestions(int offerId, CancellationToken cancellationToken) =>
            await _builder.InterviewQuestionsAsync(User, offerId, cancellationToken) is { } interview ? Ok(interview) : NotFound();

        // Brouillon de message dans une conversation du recruteur (rien n'est envoyé)
        [HttpPost("conversations/{conversationId:int}/message-draft")]
        [SwaggerResponse(StatusCodes.Status200OK, "Brouillon à relire", typeof(MessageDraftViewModel))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Type de message ou consigne manquants")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Conversation introuvable")]
        [SwaggerResponse(StatusCodes.Status409Conflict, "Pro requis, démo, assistant indisponible ou quota du mois atteint")]
        public async Task<IActionResult> DraftMessage(int conversationId, [FromBody] MessageDraftRequest model, CancellationToken cancellationToken)
        {
            var draft = await _builder.DraftMessageAsync(User, conversationId, model, cancellationToken);
            return draft == null ? NotFound() : Ok(draft);
        }
    }
}
