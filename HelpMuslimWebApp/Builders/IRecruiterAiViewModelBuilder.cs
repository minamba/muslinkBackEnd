using System.Security.Claims;
using HelpMuslimWebApp.Api.Request;
using HelpMuslimWebApp.Api.ViewModels;

namespace HelpMuslimWebApp.Api.Builders
{
    public interface IRecruiterAiViewModelBuilder
    {
        // État de l'assistant du recruteur connecté (offre Pro activée, abonné, quotas du mois)
        Task<RecruiterAiViewModel> GetAsync(ClaimsPrincipal user);

        // Offre pré-remplie à partir de notes et/ou d'une fiche de poste (PDF, Word .docx, texte)
        Task<OfferDraftViewModel> DraftOfferAsync(ClaimsPrincipal user, OfferDraftRequest model, CancellationToken cancellationToken);

        // Relecture de l'offre du formulaire : conformité, clarté, champs de la correspondance
        Task<OfferReviewViewModel> ReviewOfferAsync(ClaimsPrincipal user, OfferReviewRequest model, CancellationToken cancellationToken);

        // Analyse d'une offre du recruteur : chiffres de Muslink et conseils (null : offre introuvable ou d'un autre recruteur)
        Task<OfferInsightsViewModel?> AnalyzeOfferAsync(ClaimsPrincipal user, int offerId, CancellationToken cancellationToken);

        // Questions d'entretien pour une offre du recruteur (null : offre introuvable ou d'un autre recruteur)
        Task<InterviewQuestionsViewModel?> InterviewQuestionsAsync(ClaimsPrincipal user, int offerId, CancellationToken cancellationToken);

        // Brouillon de message à un candidat dans une conversation du recruteur (null : conversation introuvable)
        Task<MessageDraftViewModel?> DraftMessageAsync(ClaimsPrincipal user, int conversationId, MessageDraftRequest model, CancellationToken cancellationToken);
    }
}
