using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using HelpMuslimWebApp.Mailing;
using MimeKit;
using static HelpMuslimWebApp.Mailing.EmailLayout;

namespace HelpMuslimWebApp.Api.Services.Mail
{
    // Messages écrits depuis Administration › Boîte mail : le texte saisi, la signature Muslink et la citation du
    // message d'origine, dans la mise en page commune des emails (bandeau, logo, pied de page)
    public static partial class MailComposer
    {
        private static readonly CultureInfo French = CultureInfo.GetCultureInfo("fr-FR");

        // signatureHtml : la signature de l'administrateur (SignatureRenderer), absente si désactivée pour ce message
        public static EmailContent Content(string subject, string text, string? signatureHtml, MimeMessage? original)
        {
            var body = new StringBuilder(TextToHtml(text));
            if (!string.IsNullOrWhiteSpace(signatureHtml)) body.Append(signatureHtml);
            if (original != null) body.Append(QuoteHtml(original));

            return new EmailContent
            {
                Preheader = Preview(text),
                Eyebrow = "Support Muslink",
                Title = CleanSubject(subject),
                BodyHtml = body.ToString(),
                Signature = null,
                FooterNote = "Répondez simplement à cet email : votre message arrivera directement à l'équipe support de Muslink.",
            };
        }

        public static string PlainText(string text, string? signatureText, MimeMessage? original)
        {
            var plain = new StringBuilder(text.TrimEnd());
            if (!string.IsNullOrWhiteSpace(signatureText)) plain.Append($"\n\n{signatureText}");
            if (original != null)
                plain.Append($"\n\n{QuoteHeader(original)}\n").Append(string.Join('\n', OriginalText(original).Split('\n').Select(l => $"> {l}")));
            return plain.ToString();
        }

        public static string ReplySubject(string? subject) =>
            string.IsNullOrWhiteSpace(subject) ? "Re : (sans objet)"
            : ReplyPrefixRegex().IsMatch(subject) ? subject : $"Re : {subject}";

        // « Re : [Contact · Question] Mon CV » → « Mon CV » pour le titre du bandeau
        private static string CleanSubject(string subject)
        {
            var clean = ReplyPrefixRegex().Replace(subject, string.Empty);
            while (ReplyPrefixRegex().IsMatch(clean)) clean = ReplyPrefixRegex().Replace(clean, string.Empty);
            clean = ContactTagRegex().Replace(clean, string.Empty).Trim();
            return clean.Length == 0 ? "Message de l'équipe Muslink" : clean;
        }

        // Texte saisi → paragraphes, liens cliquables, retours à la ligne conservés
        private static string TextToHtml(string text)
        {
            var paragraphs = BlankLineRegex().Split(text.Replace("\r\n", "\n").Trim());
            var html = new StringBuilder();
            foreach (var paragraph in paragraphs.Where(p => !string.IsNullOrWhiteSpace(p)))
            {
                var encoded = UrlRegex().Replace(E(paragraph), m => $"<a href=\"{m.Value}\" style=\"color:{Violet};font-weight:600\">{m.Value}</a>");
                html.Append($"<p style=\"margin:0 0 16px;font-size:15.5px;line-height:1.7;color:{Ink}\">{encoded.Replace("\n", "<br>")}</p>");
            }
            return html.ToString();
        }

        private static string QuoteHtml(MimeMessage original) => $"""
            <p style="margin:30px 0 10px;font-size:13px;color:{Muted}">{E(QuoteHeader(original))}</p>
            <table role="presentation" width="100%" cellpadding="0" cellspacing="0"><tr>
              <td style="width:3px;border-radius:3px;background:{Border}">&nbsp;</td>
              <td style="padding:2px 0 2px 16px;font-size:14px;line-height:1.65;color:{Muted}">{E(OriginalText(original)).Replace("\n", "<br>")}</td>
            </tr></table>
            """;

        private static string QuoteHeader(MimeMessage original)
        {
            var from = original.ReplyTo.Mailboxes.FirstOrDefault() ?? original.From.Mailboxes.FirstOrDefault();
            var who = from == null ? "Quelqu'un" : string.IsNullOrWhiteSpace(from.Name) ? from.Address : $"{from.Name} <{from.Address}>";
            return $"Le {original.Date.ToLocalTime().ToString("dddd d MMMM yyyy 'à' HH:mm", French)}, {who} a écrit :";
        }

        // Le texte du message d'origine : sa version texte, sinon son HTML sans balises
        private static string OriginalText(MimeMessage original)
        {
            var text = original.TextBody;
            if (string.IsNullOrWhiteSpace(text) && !string.IsNullOrWhiteSpace(original.HtmlBody))
            {
                var html = StyleOrScriptRegex().Replace(original.HtmlBody, string.Empty);
                html = BlockEndRegex().Replace(html, "\n");
                text = WebUtility.HtmlDecode(TagRegex().Replace(html, string.Empty));
            }
            text = MultipleBlankLinesRegex().Replace((text ?? string.Empty).Replace("\r\n", "\n"), "\n\n").Trim();
            return text.Length > 8000 ? text[..8000] + "\n[…]" : text;
        }

        private static string Preview(string text)
        {
            var clean = string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
            return clean.Length <= 140 ? clean : clean[..140] + "…";
        }

        [GeneratedRegex(@"\n\s*\n")]
        private static partial Regex BlankLineRegex();
        [GeneratedRegex(@"https?://[^\s<>""']+")]
        private static partial Regex UrlRegex();
        [GeneratedRegex(@"^\s*(re|réf|ref|tr|fwd?)\s*:\s*", RegexOptions.IgnoreCase)]
        private static partial Regex ReplyPrefixRegex();
        [GeneratedRegex(@"^\[Contact[^\]]*\]\s*", RegexOptions.IgnoreCase)]
        private static partial Regex ContactTagRegex();
        [GeneratedRegex(@"<(style|script|head)\b[^>]*>.*?</\1\s*>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
        private static partial Regex StyleOrScriptRegex();
        [GeneratedRegex(@"<br\s*/?>|</(p|div|tr|h\d|li)>", RegexOptions.IgnoreCase)]
        private static partial Regex BlockEndRegex();
        [GeneratedRegex("<[^>]+>")]
        private static partial Regex TagRegex();
        [GeneratedRegex(@"\n\s*\n(\s*\n)+")]
        private static partial Regex MultipleBlankLinesRegex();
    }
}
