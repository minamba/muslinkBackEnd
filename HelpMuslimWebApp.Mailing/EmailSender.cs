using System.Net;
using System.Text.RegularExpressions;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace HelpMuslimWebApp.Mailing
{
    public interface IEmailSender
    {
        // Envoi immédiat : lève une exception si le serveur SMTP refuse (à utiliser quand l'appelant doit le savoir)
        Task SendAsync(EmailMessage email, CancellationToken cancellationToken = default);
    }

    public partial class EmailSender : IEmailSender
    {
        private readonly EmailSettings _settings;
        private readonly ILogger<EmailSender> _logger;

        public EmailSender(IOptions<EmailSettings> settings, ILogger<EmailSender> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task SendAsync(EmailMessage email, CancellationToken cancellationToken = default)
        {
            var message = BuildMessage(email);

            if (!_settings.IsSmtpConfigured)
            {
                await SaveToPickupDirectoryAsync(message, email, cancellationToken);
                return;
            }

            _settings.ApplyDevRedirect(message);
            var (login, password) = _settings.Credentials(_settings.NoReply)!.Value;
            using var client = new SmtpClient();
            await client.ConnectAsync(_settings.Smtp.Host!, _settings.Smtp.Port, SecureSocketOptions.Auto, cancellationToken);
            await client.AuthenticateAsync(login, password, cancellationToken);
            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
            _logger.LogInformation("Email « {Subject} » envoyé à {To}.", message.Subject, string.Join(", ", message.To.Mailboxes.Select(m => m.Address)));
        }

        private MimeMessage BuildMessage(EmailMessage email)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(email.FromName ?? _settings.NoReply.DisplayName, _settings.NoReply.Address));
            message.To.Add(new MailboxAddress(email.ToName ?? string.Empty, email.To));
            if (!string.IsNullOrWhiteSpace(email.ReplyTo))
                message.ReplyTo.Add(new MailboxAddress(email.ReplyToName ?? string.Empty, email.ReplyTo));
            message.Subject = email.Subject;

            // Version texte en plus du HTML : meilleure délivrabilité et lisible partout
            var builder = new BodyBuilder { HtmlBody = email.Html, TextBody = ToPlainText(email.Html) };
            EmailImages.Attach(builder, email.Html);
            message.Body = builder.ToMessageBody();
            return message;
        }

        // SMTP non configuré (développement) : copie locale de l'email, ou simple trace dans les logs
        private async Task SaveToPickupDirectoryAsync(MimeMessage message, EmailMessage email, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_settings.PickupDirectory))
            {
                _logger.LogInformation("SMTP non configuré — email « {Subject} » à {To} non envoyé.", email.Subject, email.To);
                return;
            }

            var directory = Path.GetFullPath(_settings.PickupDirectory);
            Directory.CreateDirectory(directory);
            var name = $"{DateTime.Now:yyyyMMdd-HHmmss-fff}-{Slug(email.Subject)}";
            await message.WriteToAsync(Path.Combine(directory, name + ".eml"), cancellationToken);
            await File.WriteAllTextAsync(Path.Combine(directory, name + ".html"), EmailImages.ToDataUris(email.Html), cancellationToken);
            _logger.LogInformation("SMTP non configuré — email « {Subject} » à {To} enregistré dans {File}.", email.Subject, email.To, Path.Combine(directory, name + ".html"));
        }

        private static string ToPlainText(string html)
        {
            // Sans l'entête (styles), le texte d'aperçu caché ni le pied de page : la version texte est aussi
            // celle qui est citée sous une réponse
            var text = HeadRegex().Replace(html, string.Empty);
            text = PreheaderRegex().Replace(text, string.Empty);
            text = FooterRegex().Replace(text, string.Empty);
            text = LinkRegex().Replace(text, m => $"{m.Groups["label"].Value} ({m.Groups["href"].Value})");
            text = BlockEndRegex().Replace(text, "\n");
            text = TagRegex().Replace(text, string.Empty);
            text = WebUtility.HtmlDecode(text);
            text = SpacesRegex().Replace(text, " ");
            return BlankLinesRegex().Replace(text, "\n\n").Trim();
        }

        private static string Slug(string value)
        {
            var slug = SlugRegex().Replace(value.ToLowerInvariant(), "-").Trim('-');
            return slug.Length > 50 ? slug[..50] : slug;
        }

        [GeneratedRegex("<head>.*?</head>", RegexOptions.Singleline | RegexOptions.IgnoreCase)]
        private static partial Regex HeadRegex();
        [GeneratedRegex("<td data-footer[^>]*>.*?</td>", RegexOptions.Singleline)]
        private static partial Regex FooterRegex();
        [GeneratedRegex("<div[^>]*data-preheader[^>]*>.*?</div>", RegexOptions.Singleline)]
        private static partial Regex PreheaderRegex();
        [GeneratedRegex("<a[^>]*href=\"(?<href>[^\"]+)\"[^>]*>(?<label>.*?)</a>", RegexOptions.Singleline)]
        private static partial Regex LinkRegex();
        [GeneratedRegex("<br\\s*/?>|</(p|tr|h1|h2|li|div)>", RegexOptions.IgnoreCase)]
        private static partial Regex BlockEndRegex();
        [GeneratedRegex("<[^>]+>")]
        private static partial Regex TagRegex();
        [GeneratedRegex("[ \\t]+")]
        private static partial Regex SpacesRegex();
        [GeneratedRegex("\\s*\\n\\s*(\\n\\s*)+")]
        private static partial Regex BlankLinesRegex();
        [GeneratedRegex("[^a-z0-9]+")]
        private static partial Regex SlugRegex();
    }
}
