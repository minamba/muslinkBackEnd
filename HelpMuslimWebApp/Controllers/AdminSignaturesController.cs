using HelpMuslimWebApp.Api.Request;
using HelpMuslimWebApp.Api.Services.Mail;
using HelpMuslimWebApp.Api.Utils;
using HelpMuslimWebApp.Domain.Models;
using HelpMuslimWebApp.Domain.Security;
using HelpMuslimWebApp.Domain.Services;
using HelpMuslimWebApp.Mailing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace HelpMuslimWebApp.Api.Controllers
{
    // Administration › Boîte mail › Signatures : modèles de signature et attribution aux administrateurs.
    // Les aperçus sont rendus ici, par le même code que l'envoi : ils montrent exactement ce que verront les destinataires.
    [ApiController]
    [Route("admin/signatures")]
    [Authorize(Policy = Permissions.Mails.Signatures)]
    public class AdminSignaturesController : Controller
    {
        private readonly ISignatureService _signatures;
        private readonly SignatureProvider _provider;

        public AdminSignaturesController(ISignatureService signatures, SignatureProvider provider)
        {
            _signatures = signatures ?? throw new ArgumentNullException(nameof(signatures));
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        // GET /admin/signatures → modèles (aperçu au nom de la personne connectée) et attributions
        [HttpGet]
        [SwaggerResponse(StatusCodes.Status200OK, "Modèles et attributions")]
        public async Task<IActionResult> Get()
        {
            var me = SignatureProvider.FullName(User.GetFirstName(), User.GetLastName());
            var myJobTitle = (await _signatures.GetAssignmentsAsync()).FirstOrDefault(a => a.UserId == User.GetUserId())?.JobTitle;
            var templates = (await _signatures.GetTemplatesAsync()).Select(t => new
            {
                t.Id, t.Name, t.Closing, t.Tagline, t.ShowJobTitle, t.ShowEmail, t.ShowWebsite, t.Phone, t.Color, t.IsDefault, t.AssignedCount,
                previewHtml = _provider.Preview(SignatureProvider.ToDesign(t), me, myJobTitle),
            });
            return Ok(new { templates, assignments = await _signatures.GetAssignmentsAsync() });
        }

        [HttpPost("templates")]
        public async Task<IActionResult> Create([FromBody] SignatureTemplateRequest request) =>
            Ok(await _signatures.AddTemplateAsync(ToModel(request)));

        [HttpPut("templates/{id:int}")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Modèle introuvable")]
        public async Task<IActionResult> Update(int id, [FromBody] SignatureTemplateRequest request)
        {
            var model = ToModel(request);
            model.Id = id;
            var updated = await _signatures.UpdateTemplateAsync(model);
            return updated == null ? NotFound(new { message = "Modèle introuvable." }) : Ok(updated);
        }

        // Les administrateurs qui l'utilisaient reviennent au modèle par défaut
        [HttpDelete("templates/{id:int}")]
        [SwaggerResponse(StatusCodes.Status409Conflict, "Le modèle par défaut ne se supprime pas")]
        public async Task<IActionResult> Delete(int id) =>
            await _signatures.DeleteTemplateAsync(id) ? Ok() : NotFound(new { message = "Modèle introuvable." });

        // POST /admin/signatures/preview → { html } : réglages en cours, au nom de la personne choisie
        [HttpPost("preview")]
        public IActionResult Preview([FromBody] SignaturePreviewRequest request) =>
            Ok(new { html = _provider.Preview(SignatureProvider.ToDesign(ToModel(request)), request.FullName, request.JobTitle) });

        // PUT /admin/signatures/assignments/{userId} { templateId, jobTitle }
        [HttpPut("assignments/{userId}")]
        public async Task<IActionResult> Assign(string userId, [FromBody] SignatureAssignmentRequest request) =>
            Ok(await _signatures.SaveAssignmentAsync(new SignatureAssignment { UserId = userId, TemplateId = request.TemplateId, JobTitle = request.JobTitle }));

        private static SignatureTemplate ToModel(SignatureTemplateRequest r) => new()
        {
            Name = r.Name,
            Closing = r.Closing,
            Tagline = r.Tagline,
            ShowJobTitle = r.ShowJobTitle,
            ShowEmail = r.ShowEmail,
            ShowWebsite = r.ShowWebsite,
            Phone = r.Phone,
            Color = r.Color ?? SignatureRenderer.DefaultColor,
            IsDefault = r.IsDefault,
        };
    }
}
