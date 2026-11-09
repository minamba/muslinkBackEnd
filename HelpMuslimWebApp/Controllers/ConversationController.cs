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
    [Route("conversations")]
    [Authorize]
    public class ConversationController : Controller
    {
        private readonly IConversationViewModelBuilder _builder;

        public ConversationController(IConversationViewModelBuilder builder)
        {
            _builder = builder ?? throw new ArgumentNullException(nameof(builder));
        }

        [HttpGet]
        [SwaggerResponse(StatusCodes.Status200OK, "Mes conversations", typeof(IEnumerable<ConversationViewModel>))]
        public async Task<IActionResult> GetConversations() => Ok(await _builder.GetMyConversationsAsync(User));

        [HttpGet("unread-count")]
        [SwaggerResponse(StatusCodes.Status200OK, "Nombre de messages non lus")]
        public async Task<IActionResult> GetUnreadCount() => Ok(new { count = await _builder.GetUnreadCountAsync(User) });

        [HttpGet("{id:int}")]
        [SwaggerResponse(StatusCodes.Status200OK, "Conversation", typeof(ConversationViewModel))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Conversation introuvable")]
        public async Task<IActionResult> GetConversation(int id)
        {
            var result = await _builder.GetConversationAsync(User, id);
            return result == null ? NotFound() : Ok(result);
        }

        [HttpGet("{id:int}/messages")]
        [SwaggerResponse(StatusCodes.Status200OK, "Messages (marqués comme lus)", typeof(IEnumerable<MessageViewModel>))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Conversation introuvable")]
        public async Task<IActionResult> GetMessages(int id)
        {
            var result = await _builder.GetMessagesAsync(User, id);
            return result == null ? NotFound() : Ok(result);
        }

        [HttpPost("{id:int}/messages")]
        [SwaggerResponse(StatusCodes.Status200OK, "Message envoyé", typeof(MessageViewModel))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Conversation introuvable")]
        public async Task<IActionResult> SendMessage(int id, [FromBody] MessageRequest model)
        {
            var result = await _builder.SendMessageAsync(User, id, model);
            return result == null ? NotFound() : Ok(result);
        }

        // Modifier son message écrit (pas un partage ni une candidature)
        [HttpPut("{id:int}/messages/{messageId:int}")]
        [SwaggerResponse(StatusCodes.Status200OK, "Message modifié", typeof(MessageViewModel))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Message vide ou trop long")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Conversation ou message introuvable")]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "Message de l'autre participant")]
        [SwaggerResponse(StatusCodes.Status409Conflict, "Partage ou candidature : non modifiable")]
        public async Task<IActionResult> EditMessage(int id, int messageId, [FromBody] MessageRequest model)
        {
            var result = await _builder.EditMessageAsync(User, id, messageId, model);
            return result == null ? NotFound() : Ok(result);
        }

        // Supprimer son message (écrit ou partage) : « Message supprimé » des deux côtés
        [HttpDelete("{id:int}/messages/{messageId:int}")]
        [SwaggerResponse(StatusCodes.Status200OK, "Message supprimé", typeof(MessageViewModel))]
        [SwaggerResponse(StatusCodes.Status403Forbidden, "Message de l'autre participant")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Conversation ou message introuvable")]
        [SwaggerResponse(StatusCodes.Status409Conflict, "Lettre de candidature : non supprimable")]
        public async Task<IActionResult> DeleteOwnMessage(int id, int messageId)
        {
            var result = await _builder.DeleteOwnMessageAsync(User, id, messageId);
            return result == null ? NotFound() : Ok(result);
        }

        // ---------- Partages ----------

        // Numéro de téléphone du profil (400 s'il n'est pas renseigné)
        [HttpPost("{id:int}/share/phone")]
        [SwaggerResponse(StatusCodes.Status200OK, "Numéro envoyé", typeof(MessageViewModel))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Aucun numéro dans le profil")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Conversation introuvable")]
        public async Task<IActionResult> SharePhone(int id) => await _builder.SharePhoneAsync(User, id) is { } m ? Ok(m) : NotFound();

        // Recruteur : fiche de poste (l'une de ses offres)
        [HttpPost("{id:int}/share/offer")]
        [Authorize(Policy = AuthorizationPolicies.Recruteur)]
        [SwaggerResponse(StatusCodes.Status200OK, "Fiche de poste envoyée", typeof(MessageViewModel))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Offre introuvable")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Conversation introuvable")]
        public async Task<IActionResult> ShareOffer(int id, [FromBody] ShareOfferRequest model) =>
            await _builder.ShareOfferAsync(User, id, model.OfferId!.Value) is { } m ? Ok(m) : NotFound();

        // Candidat : son CV (400 s'il n'en a pas)
        [HttpPost("{id:int}/share/cv")]
        [Authorize(Policy = AuthorizationPolicies.Candidat)]
        [SwaggerResponse(StatusCodes.Status200OK, "CV envoyé", typeof(MessageViewModel))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Aucun CV dans le profil")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Conversation introuvable")]
        public async Task<IActionResult> ShareCv(int id) => await _builder.ShareCvAsync(User, id) is { } m ? Ok(m) : NotFound();

        // CV envoyé dans la conversation (aperçu et téléchargement), pour ses deux participants
        [HttpGet("{id:int}/messages/{messageId:int}/cv")]
        [SwaggerResponse(StatusCodes.Status200OK, "Fichier CV")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "CV introuvable")]
        public async Task<IActionResult> GetMessageCv(int id, int messageId)
        {
            var cv = await _builder.GetMessageCvAsync(User, id, messageId);
            return cv == null ? NotFound() : File(cv.Value.Stream, cv.Value.ContentType, cv.Value.FileName);
        }

        // Recruteur : ouvrir (ou retrouver) la conversation avec un candidat
        [HttpPost("start")]
        [Authorize(Policy = AuthorizationPolicies.Recruteur)]
        [SwaggerResponse(StatusCodes.Status200OK, "Conversation", typeof(ConversationViewModel))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Candidat introuvable")]
        public async Task<IActionResult> StartConversation([FromBody] StartConversationRequest model)
        {
            var result = await _builder.StartConversationAsync(User, model);
            return result == null ? NotFound() : Ok(result);
        }

        // ---------- Administration ----------

        [HttpGet("all")]
        [Authorize(Policy = Permissions.Messages.View)]
        [SwaggerResponse(StatusCodes.Status200OK, "Toutes les conversations", typeof(IEnumerable<ConversationViewModel>))]
        public async Task<IActionResult> GetAllConversations() => Ok(await _builder.GetAllConversationsAsync());

        [HttpGet("all/{id:int}/messages")]
        [Authorize(Policy = Permissions.Messages.View)]
        [SwaggerResponse(StatusCodes.Status200OK, "Messages d'une conversation", typeof(IEnumerable<MessageViewModel>))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Conversation introuvable")]
        public async Task<IActionResult> GetConversationMessages(int id)
        {
            var result = await _builder.GetConversationMessagesAsync(id);
            return result == null ? NotFound() : Ok(result);
        }

        [HttpDelete("messages/{messageId:int}")]
        [Authorize(Policy = Permissions.Messages.Delete)]
        [SwaggerResponse(StatusCodes.Status200OK, "Message supprimé")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Message introuvable")]
        public async Task<IActionResult> DeleteMessage(int messageId) => await _builder.DeleteMessageAsync(messageId) ? Ok() : NotFound();
    }
}
