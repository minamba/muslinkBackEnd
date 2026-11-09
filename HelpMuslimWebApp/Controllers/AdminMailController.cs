using HelpMuslimWebApp.Api.Request;
using HelpMuslimWebApp.Api.Services.Mail;
using HelpMuslimWebApp.Api.Utils;
using HelpMuslimWebApp.Domain.Security;
using HelpMuslimWebApp.Mailing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MimeKit;
using Swashbuckle.AspNetCore.Annotations;

namespace HelpMuslimWebApp.Api.Controllers
{
    // Administration › Boîte mail : la boîte support@muslink.fr (réception, envoyés, brouillons)
    [ApiController]
    [Route("admin/mails")]
    [Authorize(Policy = Permissions.Mails.View)]
    public class AdminMailController : Controller
    {
        private const long MaxAttachmentsBytes = 15 * 1024 * 1024;

        private readonly ISupportMailbox _mailbox;
        private readonly SignatureProvider _signatures;

        public AdminMailController(ISupportMailbox mailbox, SignatureProvider signatures)
        {
            _mailbox = mailbox ?? throw new ArgumentNullException(nameof(mailbox));
            _signatures = signatures ?? throw new ArgumentNullException(nameof(signatures));
        }

        // GET /admin/mails/status → adresse, compteurs (non lus : badge de la barre de navigation)
        [HttpGet("status")]
        [SwaggerResponse(StatusCodes.Status200OK, "État et compteurs de la boîte")]
        public async Task<IActionResult> Status(CancellationToken ct)
        {
            var counts = _mailbox.IsConfigured ? await _mailbox.CountAsync(ct) : new MailCounts(0, 0, 0, 0);
            return Ok(new
            {
                configured = _mailbox.IsConfigured,
                address = _mailbox.Address,
                canSend = User.HasPermission(Permissions.Mails.Send),
                counts.Inbox,
                counts.Unread,
                counts.Sent,
                counts.Drafts,
                counts.Error,
                canManageSignatures = User.HasPermission(Permissions.Mails.Signatures),
                // Aperçu de la signature ajoutée aux messages de la personne connectée (son modèle, à son nom)
                signatureHtml = await _signatures.PreviewForUserAsync(User),
            });
        }

        // GET /admin/mails?folder=inbox|sent|drafts&page=1&pageSize=20
        [HttpGet]
        [SwaggerResponse(StatusCodes.Status200OK, "Une page du dossier, les plus récents d'abord", typeof(MailListResult))]
        public async Task<IActionResult> List([FromQuery] string? folder, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default) =>
            Ok(await _mailbox.ListAsync(ParseFolder(folder), page, pageSize, ct));

        // GET /admin/mails/inbox/12?images=false → message (marqué lu)
        [HttpGet("{folder}/{uid}")]
        [SwaggerResponse(StatusCodes.Status200OK, "Message assaini", typeof(MailDetail))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Message introuvable")]
        public async Task<IActionResult> Read(string folder, uint uid, [FromQuery] bool images = false, CancellationToken ct = default) =>
            await Guard(async () => await _mailbox.ReadAsync(ParseFolder(folder), uid, images, ct) is { } detail ? Ok(detail) : NotFound(new { message = "Message introuvable." }));

        [HttpGet("{folder}/{uid}/attachments/{index:int}")]
        [SwaggerResponse(StatusCodes.Status200OK, "Pièce jointe")]
        public async Task<IActionResult> Attachment(string folder, uint uid, int index, CancellationToken ct) =>
            await Guard(async () => await _mailbox.AttachmentAsync(ParseFolder(folder), uid, index, ct) is { } file
                ? File(file.Data, file.ContentType, file.FileName)
                : NotFound(new { message = "Pièce jointe introuvable." }));

        [HttpPut("{folder}/{uid}/read")]
        public async Task<IActionResult> SetRead(string folder, uint uid, [FromBody] AdminMailReadRequest request, CancellationToken ct) =>
            await Guard(async () =>
            {
                await _mailbox.SetReadAsync(ParseFolder(folder), uid, request.Read, ct);
                return Ok();
            });

        [HttpPost("delete")]
        [Authorize(Policy = Permissions.Mails.Send)]
        public async Task<IActionResult> Delete([FromBody] AdminMailDeleteRequest request, CancellationToken ct) =>
            await Guard(async () => Ok(new { deleted = await _mailbox.DeleteAsync(ParseFolder(request.Folder), request.Uids, ct) }));

        // POST /admin/mails/send (multipart) : nouveau message, réponse, ou brouillon envoyé
        [HttpPost("send")]
        [Authorize(Policy = Permissions.Mails.Send)]
        [RequestSizeLimit(25 * 1024 * 1024)]
        [SwaggerResponse(StatusCodes.Status200OK, "Message envoyé")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Destinataire ou contenu invalide")]
        [SwaggerResponse(StatusCodes.Status502BadGateway, "Le serveur mail a refusé l'envoi")]
        public async Task<IActionResult> Send([FromForm] AdminMailRequest request, CancellationToken ct)
        {
            var (mail, error) = await ToOutgoingAsync(request, forDraft: false, ct);
            if (error != null) return BadRequest(new { message = error });

            var result = await _mailbox.SendAsync(mail!, ct);
            return result.Sent
                ? Ok(new { sent = true, result.CopiedToSent })
                : StatusCode(StatusCodes.Status502BadGateway, new { message = result.Error });
        }

        // POST /admin/mails/drafts (multipart) : enregistre ou remplace un brouillon
        [HttpPost("drafts")]
        [Authorize(Policy = Permissions.Mails.Send)]
        [RequestSizeLimit(25 * 1024 * 1024)]
        public async Task<IActionResult> SaveDraft([FromForm] AdminMailRequest request, CancellationToken ct)
        {
            var (mail, error) = await ToOutgoingAsync(request, forDraft: true, ct);
            if (error != null) return BadRequest(new { message = error });
            return await Guard(async () => Ok(new { uid = await _mailbox.SaveDraftAsync(mail!, ct) }));
        }

        private async Task<(OutgoingMail? Mail, string? Error)> ToOutgoingAsync(AdminMailRequest request, bool forDraft, CancellationToken ct)
        {
            var to = Clean(request.To);
            var cc = Clean(request.Cc);
            var bcc = Clean(request.Bcc);
            var invalid = to.Concat(cc).Concat(bcc).FirstOrDefault(a => !MailboxAddress.TryParse(a, out var parsed) || !parsed.Address.Contains('@'));
            if (invalid != null) return (null, $"Adresse invalide : {invalid}");
            if (to.Count + cc.Count + bcc.Count > 30) return (null, "30 destinataires au maximum.");

            if (!forDraft)
            {
                if (to.Count == 0) return (null, "Indiquez au moins un destinataire.");
                if (string.IsNullOrWhiteSpace(request.Subject)) return (null, "L'objet est obligatoire.");
                if (string.IsNullOrWhiteSpace(request.Text)) return (null, "Le message est vide.");
            }

            if (request.Files.Sum(f => f.Length) > MaxAttachmentsBytes) return (null, "Les pièces jointes ne doivent pas dépasser 15 Mo au total.");
            var attachments = new List<MailUpload>();
            foreach (var file in request.Files)
            {
                using var memory = new MemoryStream();
                await file.CopyToAsync(memory, ct);
                attachments.Add(new MailUpload(Path.GetFileName(file.FileName), string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType, memory.ToArray()));
            }

            var (signatureHtml, signatureText) = await _signatures.ForUserAsync(User);
            return (new OutgoingMail
            {
                To = to,
                Cc = cc,
                Bcc = bcc,
                Subject = (request.Subject ?? string.Empty).ReplaceLineEndings(" ").Trim(),
                Text = request.Text ?? string.Empty,
                Signature = request.Signature,
                SignatureHtml = signatureHtml,
                SignatureText = signatureText,
                ReplyToUid = request.ReplyToUid,
                DraftUid = request.DraftUid,
                KeepDraftAttachments = request.KeepDraftAttachments,
                Attachments = attachments,
            }, null);
        }

        // Le serveur mail ne répond pas ou refuse : le message technique est utile à l'administrateur
        private static async Task<IActionResult> Guard(Func<Task<IActionResult>> action)
        {
            try
            {
                return await action();
            }
            catch (MailKit.Security.AuthenticationException)
            {
                return new ObjectResult(new { message = "Adresse ou mot de passe de la boîte refusé par le serveur mail." }) { StatusCode = StatusCodes.Status502BadGateway };
            }
            catch (Exception ex) when (ex is MailKit.ServiceNotConnectedException or MailKit.ProtocolException or MailKit.CommandException
                                       or System.Net.Sockets.SocketException or IOException or InvalidOperationException)
            {
                return new ObjectResult(new { message = $"La boîte mail ne répond pas : {ex.Message}" }) { StatusCode = StatusCodes.Status502BadGateway };
            }
        }

        private static List<string> Clean(IEnumerable<string> addresses) =>
            addresses.SelectMany(a => (a ?? string.Empty).Split([',', ';'], StringSplitOptions.RemoveEmptyEntries))
                     .Select(a => a.Trim()).Where(a => a.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        private static MailFolder ParseFolder(string? folder) => folder?.ToLowerInvariant() switch
        {
            "sent" => MailFolder.Sent,
            "drafts" => MailFolder.Drafts,
            _ => MailFolder.Inbox,
        };
    }
}
