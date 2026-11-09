using HelpMuslimWebApp.Mailing;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace HelpMuslimWebApp.Api.Services.Mail
{
    public interface ISupportMailbox
    {
        bool IsConfigured { get; }
        string Address { get; }
        Task<MailCounts> CountAsync(CancellationToken ct = default);
        Task<MailListResult> ListAsync(MailFolder folder, int page, int pageSize, CancellationToken ct = default);

        // Ouvrir un message de la réception le marque lu
        Task<MailDetail?> ReadAsync(MailFolder folder, uint uid, bool showImages, CancellationToken ct = default);
        Task<MailUpload?> AttachmentAsync(MailFolder folder, uint uid, int index, CancellationToken ct = default);
        Task SetReadAsync(MailFolder folder, uint uid, bool read, CancellationToken ct = default);

        // Vers la corbeille quand la boîte en a une (récupérable depuis le webmail)
        Task<int> DeleteAsync(MailFolder folder, IReadOnlyCollection<uint> uids, CancellationToken ct = default);
        Task<MailSendResult> SendAsync(OutgoingMail mail, CancellationToken ct = default);

        // Enregistre (ou remplace) un brouillon dans le dossier Brouillons de la boîte : renvoie son nouvel identifiant
        Task<uint?> SaveDraftAsync(OutgoingMail mail, CancellationToken ct = default);
    }

    // La boîte support@muslink.fr, lue en IMAP et répondue en SMTP.
    // Une connexion par opération : un écran consulté quelques fois par jour n'a pas besoin d'une connexion IMAP
    // tenue ouverte (que le serveur coupe après quelques minutes et qu'un seul fil peut utiliser à la fois).
    public partial class SupportMailbox : ISupportMailbox
    {
        private const string DraftReplyHeader = "X-Muslink-Reply-Uid";
        private const string DraftSignatureHeader = "X-Muslink-Signature";

        private static readonly string[] SentNames = ["Sent", "INBOX.Sent", "Sent Items", "Sent Messages", "Envoyés", "INBOX.Envoyés"];
        private static readonly string[] DraftNames = ["Drafts", "INBOX.Drafts", "Brouillons", "INBOX.Brouillons"];
        private static readonly string[] TrashNames = ["Trash", "INBOX.Trash", "Corbeille", "INBOX.Corbeille", "Deleted Items", "Deleted Messages"];

        private readonly EmailSettings _settings;
        private readonly IEmailLayout _layout;
        private readonly ILogger<SupportMailbox> _logger;

        public SupportMailbox(IOptions<EmailSettings> settings, IEmailLayout layout, ILogger<SupportMailbox> logger)
        {
            _settings = settings.Value;
            _layout = layout;
            _logger = logger;
        }

        public bool IsConfigured => _settings.IsSupportMailboxConfigured;

        public string Address => _settings.Support.Address;

        private string SiteUrl => (_settings.SiteUrl ?? string.Empty).TrimEnd('/');

        public async Task<MailCounts> CountAsync(CancellationToken ct = default)
        {
            if (!IsConfigured) return new MailCounts(0, 0, 0, 0, NotConfigured);
            try
            {
                using var client = await ConnectAsync(ct);
                // STATUS : les nombres seuls, sans ouvrir aucun dossier
                await client.Inbox.StatusAsync(StatusItems.Count | StatusItems.Unread, ct);
                var sent = await FolderAsync(client, MailFolder.Sent, ct);
                if (sent != null) await sent.StatusAsync(StatusItems.Count, ct);
                var drafts = await FolderAsync(client, MailFolder.Drafts, ct);
                if (drafts != null) await drafts.StatusAsync(StatusItems.Count, ct);
                var counts = new MailCounts(client.Inbox.Count, client.Inbox.Unread, sent?.Count ?? 0, drafts?.Count ?? 0);
                await client.DisconnectAsync(true, CancellationToken.None);
                return counts;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Boîte support : compteurs illisibles.");
                return new MailCounts(0, 0, 0, 0, Describe(ex));
            }
        }

        public async Task<MailListResult> ListAsync(MailFolder folder, int page, int pageSize, CancellationToken ct = default)
        {
            if (!IsConfigured) return new MailListResult([], 0, 0, NotConfigured);
            try
            {
                using var client = await ConnectAsync(ct);
                var box = await FolderAsync(client, folder, ct);
                if (box == null) return new MailListResult([], 0, 0);

                // Non lus de tout le dossier (STATUS se demande avant l'ouverture)
                var unread = 0;
                if (folder == MailFolder.Inbox)
                {
                    await box.StatusAsync(StatusItems.Unread, ct);
                    unread = box.Unread;
                }
                await box.OpenAsync(FolderAccess.ReadOnly, ct);
                var total = box.Count;

                // Les plus récents d'abord : les numéros IMAP suivent l'ordre d'arrivée, la page 1 est la fin du dossier
                pageSize = Math.Clamp(pageSize, 1, 50);
                var last = total - 1 - (Math.Max(1, page) - 1) * pageSize;
                if (last < 0) return new MailListResult([], total, unread);
                var first = Math.Max(0, last - pageSize + 1);

                var summaries = await box.FetchAsync(first, last,
                    MessageSummaryItems.UniqueId | MessageSummaryItems.Envelope | MessageSummaryItems.Flags
                    | MessageSummaryItems.BodyStructure | MessageSummaryItems.PreviewText | MessageSummaryItems.InternalDate, ct);

                var items = summaries.Select(s =>
                {
                    // Formulaire de contact (expéditeur no-reply) : la liste montre le visiteur, à qui l'on répond
                    var from = s.Envelope?.From?.Mailboxes?.FirstOrDefault();
                    var replyTo = s.Envelope?.ReplyTo?.Mailboxes?.FirstOrDefault();
                    var shown = from != null && replyTo != null && IsNoReply(from.Address) ? replyTo : from;
                    return new MailSummary(
                        s.UniqueId.Id,
                        shown?.Name,
                        shown?.Address,
                        (s.Envelope?.To?.Mailboxes ?? []).Select(ToAddress).ToList(),
                        s.Envelope?.Subject,
                        (s.Envelope?.Date ?? s.InternalDate ?? DateTimeOffset.UtcNow).UtcDateTime,
                        s.Flags?.HasFlag(MessageFlags.Seen) ?? false,
                        s.Flags?.HasFlag(MessageFlags.Answered) ?? false,
                        s.Attachments?.Any() ?? false,
                        Shorten(s.PreviewText, 160));
                }).OrderByDescending(m => m.Date).ToList();

                await client.DisconnectAsync(true, CancellationToken.None);
                return new MailListResult(items, total, unread);
            }
            catch (Exception ex)
            {
                // Le message technique remonte à l'écran : seul un administrateur le voit, et il dit où chercher
                _logger.LogError(ex, "Boîte support : lecture du dossier {Folder} impossible.", folder);
                return new MailListResult([], 0, 0, Describe(ex));
            }
        }

        public async Task<MailDetail?> ReadAsync(MailFolder folder, uint uid, bool showImages, CancellationToken ct = default)
        {
            if (!IsConfigured) return null;
            using var client = await ConnectAsync(ct);
            var box = await FolderAsync(client, folder, ct);
            if (box == null) return null;
            await box.OpenAsync(folder == MailFolder.Inbox ? FolderAccess.ReadWrite : FolderAccess.ReadOnly, ct);

            var id = new UniqueId(uid);
            MimeMessage message;
            try
            {
                message = await box.GetMessageAsync(id, ct);
            }
            catch (MessageNotFoundException)
            {
                return null;
            }
            var summary = (await box.FetchAsync([id], MessageSummaryItems.Flags | MessageSummaryItems.InternalDate, ct)).FirstOrDefault();
            var flags = summary?.Flags ?? MessageFlags.None;
            // Sans entête Date (rare), la date de réception par le serveur
            var date = message.Date != DateTimeOffset.MinValue ? message.Date : summary?.InternalDate ?? DateTimeOffset.UtcNow;

            var imagesBlocked = false;
            var html = string.IsNullOrWhiteSpace(message.HtmlBody) ? null : MailHtmlSanitizer.Sanitize(message.HtmlBody, message, showImages, out imagesBlocked);
            var from = message.From.Mailboxes.FirstOrDefault();
            var replyTo = message.ReplyTo.Mailboxes.FirstOrDefault();

            var detail = new MailDetail
            {
                Uid = uid,
                Folder = folder,
                FromName = from?.Name,
                FromAddress = from?.Address,
                ReplyTo = replyTo != null && !string.Equals(replyTo.Address, from?.Address, StringComparison.OrdinalIgnoreCase) ? ToAddress(replyTo) : null,
                To = message.To.Mailboxes.Select(ToAddress).ToList(),
                Cc = message.Cc.Mailboxes.Select(ToAddress).ToList(),
                Bcc = message.Bcc.Mailboxes.Select(ToAddress).ToList(),
                Subject = message.Subject,
                Date = date.UtcDateTime,
                IsAnswered = flags.HasFlag(MessageFlags.Answered),
                Html = html,
                Text = message.TextBody,
                ImagesBlocked = imagesBlocked,
                Attachments = Attachments(message).Select((p, i) => new MailAttachmentInfo(i, p.FileName ?? $"piece-jointe-{i + 1}", p.ContentType?.MimeType ?? "application/octet-stream", DecodedSize(p))).ToList(),
                DraftReplyToUid = uint.TryParse(message.Headers[DraftReplyHeader], out var replyUid) ? replyUid : null,
                DraftSignature = message.Headers[DraftSignatureHeader] != "0",
            };

            if (folder == MailFolder.Inbox && !flags.HasFlag(MessageFlags.Seen))
                await box.AddFlagsAsync(id, MessageFlags.Seen, true, ct);
            await client.DisconnectAsync(true, CancellationToken.None);
            return detail;
        }

        public async Task<MailUpload?> AttachmentAsync(MailFolder folder, uint uid, int index, CancellationToken ct = default)
        {
            if (!IsConfigured) return null;
            using var client = await ConnectAsync(ct);
            var box = await FolderAsync(client, folder, ct);
            if (box == null) return null;
            await box.OpenAsync(FolderAccess.ReadOnly, ct);
            var message = await box.GetMessageAsync(new UniqueId(uid), ct);
            var part = Attachments(message).ElementAtOrDefault(index);
            if (part == null) return null;

            using var memory = new MemoryStream();
            await part.Content!.DecodeToAsync(memory, ct);
            await client.DisconnectAsync(true, CancellationToken.None);
            return new MailUpload(part.FileName ?? $"piece-jointe-{index + 1}", part.ContentType?.MimeType ?? "application/octet-stream", memory.ToArray());
        }

        public async Task SetReadAsync(MailFolder folder, uint uid, bool read, CancellationToken ct = default)
        {
            if (!IsConfigured) return;
            using var client = await ConnectAsync(ct);
            var box = await FolderAsync(client, folder, ct);
            if (box == null) return;
            await box.OpenAsync(FolderAccess.ReadWrite, ct);
            if (read) await box.AddFlagsAsync(new UniqueId(uid), MessageFlags.Seen, true, ct);
            else await box.RemoveFlagsAsync(new UniqueId(uid), MessageFlags.Seen, true, ct);
            await client.DisconnectAsync(true, CancellationToken.None);
        }

        public async Task<int> DeleteAsync(MailFolder folder, IReadOnlyCollection<uint> uids, CancellationToken ct = default)
        {
            if (!IsConfigured || uids.Count == 0) return 0;
            using var client = await ConnectAsync(ct);
            var box = await FolderAsync(client, folder, ct);
            if (box == null) return 0;
            await box.OpenAsync(FolderAccess.ReadWrite, ct);
            var ids = uids.Distinct().Select(u => new UniqueId(u)).ToList();

            var trash = await SpecialFolderAsync(client, SpecialFolder.Trash, TrashNames, ct);
            if (trash != null && trash.FullName != box.FullName && folder != MailFolder.Drafts)
            {
                await box.MoveToAsync(ids, trash, ct);
            }
            else
            {
                await box.AddFlagsAsync(ids, MessageFlags.Deleted, true, ct);
                await box.ExpungeAsync(ids, ct);
            }
            await client.DisconnectAsync(true, CancellationToken.None);
            _logger.LogInformation("Boîte support : {Count} message(s) supprimé(s) de {Folder}.", ids.Count, folder);
            return ids.Count;
        }

        public async Task<MailSendResult> SendAsync(OutgoingMail mail, CancellationToken ct = default)
        {
            if (!IsConfigured) return new MailSendResult(false, NotConfigured);
            if (string.IsNullOrWhiteSpace(_settings.Smtp.Host) || _settings.Credentials(_settings.Support) is not { } credentials)
                return new MailSendResult(false, "Le serveur d'envoi (Email:Smtp) n'est pas configuré.");

            var refused = mail.To.Concat(mail.Cc).Concat(mail.Bcc).FirstOrDefault(DoesNotReceive);
            if (refused != null)
            {
                return new MailSendResult(false, $"{refused} ne reçoit pas de réponses. S'il s'agit d'un message du formulaire de contact, "
                    + "répondez à l'adresse du visiteur (indiquée dans « Répondre à »).");
            }

            try
            {
                // Une seule connexion IMAP pour toute l'opération : les hébergeurs mutualisés limitent les connexions
                using var imap = await ConnectAsync(ct);
                MimeMessage? original = null;
                IMailFolder? inbox = null;
                if (mail.ReplyToUid is uint replyUid)
                {
                    inbox = imap.Inbox;
                    await inbox.OpenAsync(FolderAccess.ReadWrite, ct);
                    original = await inbox.GetMessageAsync(new UniqueId(replyUid), ct);
                }

                var draftAttachments = await DraftAttachmentsAsync(imap, mail, ct);
                var message = Build(mail, original, draftAttachments, asDraft: false);
                _settings.ApplyDevRedirect(message);

                using (var smtp = new SmtpClient())
                {
                    await smtp.ConnectAsync(_settings.Smtp.Host, _settings.Smtp.Port, SecureSocketOptions.Auto, ct);
                    await smtp.AuthenticateAsync(credentials.Login, credentials.Password, ct);
                    // La réponse du serveur (identifiant de file d'attente) est journalisée : c'est elle qu'on cite à
                    // l'hébergeur quand un message accepté n'arrive jamais
                    var receipt = await smtp.SendAsync(message, ct);
                    await smtp.DisconnectAsync(true, CancellationToken.None);
                    _logger.LogInformation("Boîte support : message « {Subject} » envoyé à {To}. Réponse du serveur : {Receipt}",
                        message.Subject, string.Join(", ", mail.To), receipt);
                }

                // Le message est parti : la suite (drapeau, copie, brouillon) ne doit plus faire croire à un échec
                if (inbox != null && mail.ReplyToUid is uint answered)
                    await Tolerate(() => inbox.AddFlagsAsync(new UniqueId(answered), MessageFlags.Answered | MessageFlags.Seen, true, ct), "drapeau « répondu »");

                // Une copie dans « Envoyés », comme tout client mail : sans elle, une réponse partie d'ici n'existe nulle part
                var copied = await Tolerate(async () =>
                {
                    var sent = await FolderAsync(imap, MailFolder.Sent, ct) ?? await CreateFolderAsync(imap, "Sent", ct);
                    await sent.AppendAsync(message, MessageFlags.Seen, ct);
                    return true;
                }, "copie dans Envoyés");

                if (mail.DraftUid is uint draftUid)
                    await Tolerate(() => RemoveDraftAsync(imap, draftUid, ct), "suppression du brouillon");

                await imap.DisconnectAsync(true, CancellationToken.None);
                return new MailSendResult(true, CopiedToSent: copied);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Boîte support : envoi de « {Subject} » impossible.", mail.Subject);
                return new MailSendResult(false, Describe(ex));
            }
        }

        public async Task<uint?> SaveDraftAsync(OutgoingMail mail, CancellationToken ct = default)
        {
            if (!IsConfigured) throw new InvalidOperationException(NotConfigured);
            using var imap = await ConnectAsync(ct);
            var drafts = await FolderAsync(imap, MailFolder.Drafts, ct) ?? await CreateFolderAsync(imap, "Drafts", ct);

            var draftAttachments = await DraftAttachmentsAsync(imap, mail, ct);
            var message = Build(mail, null, draftAttachments, asDraft: true);

            // IMAP ne modifie pas un message : le nouveau brouillon est ajouté, puis l'ancien supprimé
            var uid = await drafts.AppendAsync(message, MessageFlags.Draft | MessageFlags.Seen, ct);
            if (mail.DraftUid is uint previous) await Tolerate(() => RemoveDraftAsync(imap, previous, ct), "remplacement du brouillon");
            await imap.DisconnectAsync(true, CancellationToken.None);
            return uid?.Id;
        }

        private MimeMessage Build(OutgoingMail mail, MimeMessage? original, IReadOnlyList<MimePart> draftAttachments, bool asDraft)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.Support.DisplayName, _settings.Support.Address));
            foreach (var to in mail.To) message.To.Add(MailboxAddress.Parse(to));
            foreach (var cc in mail.Cc) message.Cc.Add(MailboxAddress.Parse(cc));
            foreach (var bcc in mail.Bcc) message.Bcc.Add(MailboxAddress.Parse(bcc));
            message.Subject = mail.Subject;
            message.Date = DateTimeOffset.Now;

            // Les entêtes du fil : sans elles, la réponse arrive détachée de la question chez le destinataire
            if (original != null && !string.IsNullOrWhiteSpace(original.MessageId))
            {
                message.InReplyTo = original.MessageId;
                foreach (var reference in original.References) message.References.Add(reference);
                message.References.Add(original.MessageId);
            }

            var builder = new BodyBuilder();
            if (asDraft)
            {
                // Brouillon : le texte tel que saisi, la signature et la citation seront ajoutées à l'envoi
                builder.TextBody = mail.Text;
                if (mail.ReplyToUid is uint replyUid) message.Headers.Add(DraftReplyHeader, replyUid.ToString());
                message.Headers.Add(DraftSignatureHeader, mail.Signature ? "1" : "0");
            }
            else
            {
                // Même mise en page que les emails automatiques ; logos joints en pièces liées
                var html = _layout.Render(MailComposer.Content(mail.Subject, mail.Text, mail.Signature ? mail.SignatureHtml : null, original));
                builder.HtmlBody = html;
                builder.TextBody = MailComposer.PlainText(mail.Text, mail.Signature ? mail.SignatureText : null, original);
                EmailImages.Attach(builder, html);
            }

            foreach (var part in draftAttachments)
            {
                using var memory = new MemoryStream();
                part.Content!.DecodeTo(memory);
                builder.Attachments.Add(part.FileName ?? "piece-jointe", memory.ToArray(), part.ContentType);
            }
            foreach (var upload in mail.Attachments)
                builder.Attachments.Add(upload.FileName, upload.Data, ContentType.Parse(upload.ContentType));

            message.Body = builder.ToMessageBody();
            return message;
        }

        // Pièces jointes conservées d'un brouillon repris
        private async Task<IReadOnlyList<MimePart>> DraftAttachmentsAsync(ImapClient imap, OutgoingMail mail, CancellationToken ct)
        {
            if (mail.DraftUid is not uint draftUid || mail.KeepDraftAttachments.Count == 0) return [];
            var drafts = await FolderAsync(imap, MailFolder.Drafts, ct);
            if (drafts == null) return [];
            await drafts.OpenAsync(FolderAccess.ReadOnly, ct);
            var draft = await drafts.GetMessageAsync(new UniqueId(draftUid), ct);
            var keep = mail.KeepDraftAttachments.ToHashSet();
            return Attachments(draft).Where((_, i) => keep.Contains(i)).ToList();
        }

        private async Task RemoveDraftAsync(ImapClient imap, uint uid, CancellationToken ct)
        {
            var drafts = await FolderAsync(imap, MailFolder.Drafts, ct);
            if (drafts == null) return;
            await drafts.OpenAsync(FolderAccess.ReadWrite, ct);
            await drafts.AddFlagsAsync(new UniqueId(uid), MessageFlags.Deleted, true, ct);
            await drafts.ExpungeAsync([new UniqueId(uid)], ct);
        }

        private async Task<ImapClient> ConnectAsync(CancellationToken ct)
        {
            var client = new ImapClient();
            // IsConfigured a déjà vérifié le serveur et le mot de passe. Auto : 993 = TLS dès la connexion (LWS),
            // autre port = STARTTLS si le serveur le propose
            await client.ConnectAsync(_settings.ImapHost!, _settings.Imap.Port, SecureSocketOptions.Auto, ct);
            await client.AuthenticateAsync(_settings.Support.Address, _settings.Support.Password!, ct);
            return client;
        }

        private static async Task<IMailFolder?> FolderAsync(ImapClient client, MailFolder folder, CancellationToken ct) => folder switch
        {
            MailFolder.Sent => await SpecialFolderAsync(client, SpecialFolder.Sent, SentNames, ct),
            MailFolder.Drafts => await SpecialFolderAsync(client, SpecialFolder.Drafts, DraftNames, ct),
            _ => client.Inbox,
        };

        // Le dossier que le serveur déclare d'abord (son nom varie selon la langue de l'hébergeur), sinon les noms courants
        private static async Task<IMailFolder?> SpecialFolderAsync(ImapClient client, SpecialFolder special, string[] names, CancellationToken ct)
        {
            try
            {
                var declared = client.GetFolder(special);
                if (declared != null) return declared;
            }
            catch (NotSupportedException) { }

            foreach (var name in names)
            {
                try
                {
                    var folder = await client.GetFolderAsync(name, ct);
                    if (folder != null) return folder;
                }
                catch (FolderNotFoundException) { }
            }
            return null;
        }

        // Boîte neuve sans « Envoyés » ou « Brouillons » : le dossier est créé à la racine personnelle
        private static async Task<IMailFolder> CreateFolderAsync(ImapClient client, string name, CancellationToken ct)
        {
            var root = client.GetFolder(client.PersonalNamespaces[0]);
            return await root.CreateAsync(name, true, ct) ?? throw new InvalidOperationException($"Création du dossier « {name} » refusée par le serveur mail.");
        }

        private static IEnumerable<MimePart> Attachments(MimeMessage message) => message.Attachments.OfType<MimePart>();

        private static long DecodedSize(MimePart part)
        {
            try
            {
                using var memory = new MemoryStream();
                part.Content?.DecodeTo(memory);
                return memory.Length;
            }
            catch
            {
                return 0;
            }
        }

        // no-reply et la boîte elle-même ne lisent pas leurs emails : y répondre enverrait dans le vide
        private bool DoesNotReceive(string address) =>
            IsNoReply(address) || string.Equals(address, _settings.Support.Address, StringComparison.OrdinalIgnoreCase);

        private static bool IsNoReply(string address)
        {
            var local = address.Split('@')[0].ToLowerInvariant().Replace("-", "").Replace("_", "").Replace(".", "");
            return local is "noreply" or "donotreply" or "nepasrepondre";
        }

        private async Task<T> Tolerate<T>(Func<Task<T>> action, string what)
        {
            try
            {
                return await action();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Boîte support : {What} impossible (le message est bien parti).", what);
                return default!;
            }
        }

        private Task Tolerate(Func<Task> action, string what) => Tolerate(async () => { await action(); return true; }, what);

        private static MailAddress ToAddress(MailboxAddress m) => new(string.IsNullOrWhiteSpace(m.Name) ? null : m.Name, m.Address);

        // L'aperçu IMAP d'un email HTML reprend parfois le contenu de ses balises <style> : les règles CSS sont retirées
        private static string? Shorten(string? text, int max)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            text = CssBlockRegex().Replace(text, " ");
            var clean = string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
            if (clean.Length == 0) return null;
            return clean.Length <= max ? clean : clean[..max] + "…";
        }

        // @media (…) { .a { … } .b { … } } puis règles simples .a { … }
        [System.Text.RegularExpressions.GeneratedRegex(@"@media[^{]*\{(?:[^{}]*\{[^{}]*\})*[^{}]*\}|[^\s{}][^{}]{0,80}\{[^{}]*\}")]
        private static partial System.Text.RegularExpressions.Regex CssBlockRegex();

        private const string NotConfigured = "La boîte support n'est pas configurée (Email:Imap et Email:Support:Password).";

        private static string Describe(Exception ex) => ex switch
        {
            AuthenticationException => "Adresse ou mot de passe de la boîte refusé par le serveur mail.",
            SslHandshakeException => "Connexion TLS refusée : vérifiez le nom du serveur (chez LWS, mailXX.lwspanel.com et non mail.muslink.fr).",
            _ => $"{ex.GetType().Name} : {ex.Message}",
        };
    }
}
