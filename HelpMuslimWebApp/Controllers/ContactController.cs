using System.Text.RegularExpressions;
using HelpMuslimWebApp.Api.Request;
using HelpMuslimWebApp.Api.Services;
using HelpMuslimWebApp.Api.Services.Telegram;
using HelpMuslimWebApp.Api.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Swashbuckle.AspNetCore.Annotations;

namespace HelpMuslimWebApp.Api.Controllers
{
    // Formulaire de contact : le message part vers support@, le visiteur reçoit un accusé de réception
    [ApiController]
    [Route("contact")]
    [AllowAnonymous]
    public partial class ContactController : Controller
    {
        public const string RateLimitPolicy = "contact";

        private readonly IEmailService _emailService;
        private readonly ITelegramNotifier _telegram;
        private readonly ILogger<ContactController> _logger;

        public ContactController(IEmailService emailService, ITelegramNotifier telegram, ILogger<ContactController> logger)
        {
            _telegram = telegram;
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
            _logger = logger;
        }

        [HttpPost]
        [EnableRateLimiting(RateLimitPolicy)]
        [SwaggerResponse(StatusCodes.Status200OK, "Message transmis au support")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Champ manquant ou invalide")]
        [SwaggerResponse(StatusCodes.Status429TooManyRequests, "Trop de messages envoyés")]
        [SwaggerResponse(StatusCodes.Status503ServiceUnavailable, "Envoi impossible pour le moment")]
        public async Task<IActionResult> Send([FromBody] ContactRequest request)
        {
            // Robot (champ piège rempli) : réponse identique à un succès, rien n'est envoyé
            if (!string.IsNullOrWhiteSpace(request.Website)) return Ok();

            if (!ContactRequest.Reasons.Contains(request.Reason))
                return BadRequest(new { message = "Choisissez un motif dans la liste." });

            var name = SingleLine(request.Name!);
            var email = request.Email!.Trim();

            // Visiteur connecté : le support voit de quel compte il s'agit
            string? account = null;
            if (User.Identity?.IsAuthenticated == true)
                account = $"{User.GetAccountType()} · {User.GetEmail()} (id {User.GetUserId()})";

            var subject = SingleLine(request.Subject!);
            var message = request.Message!.Trim();
            try
            {
                await _emailService.SendContactMessageAsync(name, email, request.Reason!, subject, message, account);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Message de contact de {Email} non transmis au support.", email);
                return StatusCode(StatusCodes.Status503ServiceUnavailable,
                    new { message = "Votre message n'a pas pu être envoyé pour le moment. Réessayez dans quelques minutes." });
            }

            _telegram.ContactMessage(name, email, request.Reason!, subject, message, account);
            return Ok();
        }

        // Pas de retour à la ligne dans un nom ou un sujet d'email
        private static string SingleLine(string value) => WhitespaceRegex().Replace(value, " ").Trim();

        [GeneratedRegex(@"\s+")]
        private static partial Regex WhitespaceRegex();
    }
}
