using System.Text;
using Microsoft.Extensions.Options;

namespace HelpMuslimWebApp.Mailing
{
    // Contenu d'un email ; la mise en page (couleurs, logo, pied de page) est commune à tous les emails
    public class EmailContent
    {
        // Texte d'aperçu affiché par la boîte de réception à côté du sujet
        public string? Preheader { get; init; }

        // Pastille dorée et titre du bandeau violet
        public string? Eyebrow { get; init; }
        public required string Title { get; init; }

        public string? Greeting { get; init; }

        // HTML : les valeurs saisies par les utilisateurs doivent passer par EmailLayout.E
        public IReadOnlyList<string> Paragraphs { get; init; } = [];

        // Bloc HTML déjà mis en forme, inséré tel quel après les paragraphes (message écrit depuis la boîte support)
        public string? BodyHtml { get; init; }

        // Étapes numérotées (texte brut)
        public IReadOnlyList<EmailStep> Steps { get; init; } = [];

        // Fiche « libellé : valeur » (texte brut)
        public IReadOnlyList<KeyValuePair<string, string>> Details { get; init; } = [];

        // Citation encadrée (texte brut, sauts de ligne conservés)
        public string? Quote { get; init; }

        public EmailButton? Button { get; init; }
        public EmailButton? SecondaryLink { get; init; }

        // Paragraphe après le bouton
        public string? Closing { get; init; }

        public string? Signature { get; init; } = "L'équipe Muslink";

        // Remplace la mention « email automatique, ne pas répondre » du pied de page
        public string? FooterNote { get; init; }
    }

    public record EmailStep(string Title, string Text);

    public record EmailButton(string Label, string Url);

    public interface IEmailLayout
    {
        string Render(EmailContent content);

        // Lien absolu vers une page du site (/offres/12 → https://muslink.fr/offres/12)
        string SiteLink(string path);
    }

    // Styles en ligne et tableaux : seule mise en page fiable dans Gmail, Outlook et les applis mobiles.
    // Les dégradés ont toujours une couleur unie de repli (Outlook les ignore).
    public class EmailLayout : IEmailLayout
    {
        public const string Violet = "#4c1d95";
        public const string VioletDeep = "#2e1065";
        public const string VioletDark = "#1e0a3c";
        public const string VioletBright = "#7c3aed";
        public const string Gold = "#f5c451";
        public const string GoldDark = "#b7861b";
        public const string Ink = "#1f1633";
        public const string Muted = "#6b6280";
        public const string Page = "#f4f2f9";
        public const string Soft = "#f8f6fc";
        public const string Border = "#ebe5f5";
        public const string Font = "'Inter','Segoe UI',Roboto,Helvetica,Arial,sans-serif";

        // Téléphone : marges et titre réduits (ignoré sans risque par les clients qui ne lisent pas les media queries)
        private const string MobileStyles =
            "@media only screen and (max-width:520px){.mx-pad{padding-left:24px!important;padding-right:24px!important}"
            + ".mx-head{padding-top:30px!important;padding-bottom:34px!important}.mx-title{font-size:24px!important}"
            + ".mx-btn a{display:block!important;padding:16px 20px!important}}";

        private readonly EmailSettings _settings;

        public EmailLayout(IOptions<EmailSettings> settings)
        {
            _settings = settings.Value;
        }

        private string SiteUrl => (_settings.SiteUrl ?? string.Empty).TrimEnd('/');

        public string SiteLink(string path) => $"{SiteUrl}/{path.TrimStart('/')}";

        // Échappe uniquement les caractères spéciaux HTML : les accents restent lisibles (UTF-8)
        public static string E(string? value) => (value ?? string.Empty)
            .Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;").Replace("'", "&#39;");

        public string Render(EmailContent content)
        {
            var body = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(content.Greeting))
                body.Append($"""<p style="margin:0 0 18px;font-size:17px;line-height:1.5;font-weight:800;color:{Ink}">{E(content.Greeting)}</p>""");
            foreach (var paragraph in content.Paragraphs)
                body.Append(Paragraph(paragraph));
            if (!string.IsNullOrWhiteSpace(content.BodyHtml))
                body.Append(content.BodyHtml);
            if (content.Details.Count > 0)
                body.Append(Details(content.Details));
            if (!string.IsNullOrWhiteSpace(content.Quote))
                body.Append(Quote(content.Quote));
            if (content.Steps.Count > 0)
                body.Append(Steps(content.Steps));
            if (content.Button != null)
                body.Append(Button(content.Button));
            if (content.SecondaryLink != null)
                body.Append($"""<p style="margin:16px 0 0;font-size:14px;text-align:center"><a href="{E(content.SecondaryLink.Url)}" style="color:{Violet};font-weight:700;text-decoration:none;border-bottom:2px solid {Gold};padding-bottom:1px">{E(content.SecondaryLink.Label)}</a></p>""");
            if (!string.IsNullOrWhiteSpace(content.Closing))
                body.Append($"""<p style="margin:30px 0 0;font-size:15px;line-height:1.7;color:{Ink}">{content.Closing}</p>""");
            if (!string.IsNullOrWhiteSpace(content.Signature))
                body.Append($"""<p style="margin:32px 0 0;font-size:15px;line-height:1.6;color:{Ink}">À bientôt,<br><strong style="color:{Violet}">{E(content.Signature)}</strong></p>""");

            var footerNote = content.FooterNote
                ?? $"Cet email vous a été envoyé automatiquement, merci de ne pas y répondre. Une question ? <a href=\"{E(SiteLink("/contact"))}\" style=\"color:{Muted};text-decoration:underline\">Contactez-nous</a> ou écrivez à <a href=\"mailto:{E(_settings.Support.Address)}\" style=\"color:{Muted};text-decoration:underline\">{E(_settings.Support.Address)}</a>.";

            var eyebrow = string.IsNullOrWhiteSpace(content.Eyebrow) ? "" : $"""
                <table role="presentation" cellpadding="0" cellspacing="0" align="center" style="margin:30px auto 0"><tr>
                  <td style="padding:7px 16px;border:1px solid rgba(245,196,81,.55);border-radius:999px;background-color:rgba(245,196,81,.10);font-size:11.5px;font-weight:700;letter-spacing:1.8px;text-transform:uppercase;color:{Gold};font-family:{Font}">{E(content.Eyebrow)}</td>
                </tr></table>
                """;

            return $"""
                <!doctype html>
                <html lang="fr">
                <head>
                  <meta charset="utf-8">
                  <meta name="viewport" content="width=device-width,initial-scale=1">
                  <meta name="color-scheme" content="light">
                  <meta name="supported-color-schemes" content="light">
                  <link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;600;700;800&display=swap" rel="stylesheet">
                  <style>{MobileStyles}</style>
                </head>
                <body style="margin:0;padding:0;background:{Page};font-family:{Font};color:{Ink};-webkit-text-size-adjust:100%">
                  <div data-preheader style="display:none;max-height:0;overflow:hidden;opacity:0;mso-hide:all">{E(content.Preheader ?? content.Title)}&#8199;&#847;&#8199;&#847;&#8199;&#847;&#8199;&#847;&#8199;&#847;&#8199;&#847;</div>
                  <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background:{Page};font-family:{Font}">
                    <tr><td align="center" style="padding:36px 12px 28px;font-family:{Font}">
                      <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="max-width:600px;font-family:{Font}">

                        <!-- Liseré violet → or -->
                        <tr><td style="height:6px;line-height:6px;font-size:0;border-radius:24px 24px 0 0;background-color:{Gold};background-image:linear-gradient(90deg,{VioletBright} 0%,{Violet} 45%,{Gold} 100%)">&nbsp;</td></tr>

                        <!-- Bandeau -->
                        <tr><td class="mx-head mx-pad" align="center" style="background-color:{VioletDark};background-image:radial-gradient(circle at 88% 0%,rgba(245,196,81,.30) 0%,rgba(245,196,81,0) 42%),radial-gradient(circle at 0% 100%,rgba(124,58,237,.55) 0%,rgba(124,58,237,0) 50%),linear-gradient(160deg,{VioletDark} 0%,{VioletDeep} 55%,{Violet} 100%);padding:38px 44px 44px;text-align:center">
                          <img src="{EmailImages.Src(EmailImages.HeaderLogo)}" width="150" height="32" alt="Muslink" style="display:block;margin:0 auto;border:0;height:32px;width:150px;color:#ffffff;font-size:26px;font-weight:800">
                          {eyebrow}
                          <h1 class="mx-title" style="margin:{(string.IsNullOrWhiteSpace(content.Eyebrow) ? "30px" : "16px")} 0 0;font-family:{Font};font-size:29px;line-height:1.25;font-weight:800;color:#ffffff;letter-spacing:-0.4px">{E(Typography(content.Title))}</h1>
                          <table role="presentation" cellpadding="0" cellspacing="0" align="center" style="margin:18px auto 0"><tr>
                            <td style="width:28px;height:4px;line-height:4px;font-size:0;border-radius:2px;background:{Gold}">&nbsp;</td>
                            <td style="width:6px;font-size:0">&nbsp;</td>
                            <td style="width:8px;height:4px;line-height:4px;font-size:0;border-radius:2px;background:{Gold};opacity:.55">&nbsp;</td>
                          </tr></table>
                        </td></tr>

                        <!-- Contenu -->
                        <tr><td class="mx-pad" style="font-family:{Font};color:{Ink};word-break:break-word;background:#ffffff;border-left:1px solid {Border};border-right:1px solid {Border};border-bottom:1px solid {Border};border-radius:0 0 24px 24px;padding:40px 44px 44px;box-shadow:0 18px 40px -24px rgba(46,16,101,.35)">
                          {body}
                        </td></tr>

                        <!-- Pied de page (retiré de la version texte et des citations) -->
                        <tr><td data-footer align="center" style="padding:30px 24px 0;font-family:{Font};text-align:center">
                          <img src="{EmailImages.Src(EmailImages.SignatureLogo)}" width="34" height="34" alt="" style="display:block;margin:0 auto 10px;border:0;width:34px;height:34px">
                          <p style="margin:0;font-size:13px;font-weight:800;color:{Violet}">Muslink</p>
                          <p style="margin:2px 0 14px;font-size:12.5px;color:{Muted}">Trouvez l'opportunité qui vous correspond</p>
                          <p style="margin:0 0 16px;font-size:12.5px">
                            {FooterLink("Offres", "/offres")} <span style="color:#c9c0da">&nbsp;•&nbsp;</span>
                            {FooterLink("Entreprises", "/entreprises")} <span style="color:#c9c0da">&nbsp;•&nbsp;</span>
                            {FooterLink("Actualités", "/actualites")} <span style="color:#c9c0da">&nbsp;•&nbsp;</span>
                            {FooterLink("Contact", "/contact")}
                          </p>
                          <p style="margin:0 auto;max-width:460px;font-size:12px;line-height:1.7;color:{Muted}">{footerNote}</p>
                          <p style="margin:14px 0 0;font-size:11.5px;color:#a69cba">© {DateTime.UtcNow.Year} Muslink · <a href="{E(SiteUrl)}" style="color:#a69cba;text-decoration:none">muslink.fr</a></p>
                        </td></tr>

                      </table>
                    </td></tr>
                  </table>
                </body>
                </html>
                """;
        }

        // Typographie française : espace insécable avant ? ! : ; (le signe ne part pas seul à la ligne)
        private static string Typography(string text) =>
            System.Text.RegularExpressions.Regex.Replace(text, @"\s+([?!:;»])", " $1").Replace("« ", "« ");

        private string FooterLink(string label, string path) =>
            $"""<a href="{E(SiteLink(path))}" style="color:{Violet};font-weight:600;text-decoration:none">{label}</a>""";

        private static string Paragraph(string html) =>
            $"""<p style="margin:0 0 16px;font-size:15.5px;line-height:1.7;color:{Ink}">{html}</p>""";

        private static string Steps(IReadOnlyList<EmailStep> steps)
        {
            var rows = new StringBuilder();
            for (var i = 0; i < steps.Count; i++)
            {
                var last = i == steps.Count - 1;
                rows.Append($"""
                    <tr>
                      <td width="52" valign="top" style="padding:{(i == 0 ? 0 : 18)}px 16px 0 0">
                        <table role="presentation" cellpadding="0" cellspacing="0"><tr>
                          <td align="center" valign="middle" style="width:36px;height:36px;border-radius:12px;background-color:{Violet};background-image:linear-gradient(135deg,{VioletDeep} 0%,{Violet} 55%,{VioletBright} 100%);color:{Gold};font-family:{Font};font-weight:800;font-size:15px;box-shadow:0 8px 16px -8px rgba(76,29,149,.7)">{i + 1}</td>
                        </tr></table>
                      </td>
                      <td valign="top" style="padding:{(i == 0 ? 0 : 18)}px 0 {(last ? 0 : 18)}px;{(last ? "" : $"border-bottom:1px dashed {Border};")}">
                        <p style="margin:0;font-size:15px;font-weight:700;color:{Ink}">{E(steps[i].Title)}</p>
                        <p style="margin:4px 0 0;font-size:14px;line-height:1.6;color:{Muted}">{E(steps[i].Text)}</p>
                      </td>
                    </tr>
                    """);
            }
            return $"""<table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="margin:10px 0 8px;background:{Soft};border:1px solid {Border};border-radius:18px"><tr><td style="padding:24px 24px 22px"><table role="presentation" width="100%" cellpadding="0" cellspacing="0">{rows}</table></td></tr></table>""";
        }

        private static string Details(IReadOnlyList<KeyValuePair<string, string>> details)
        {
            var rows = new StringBuilder();
            for (var i = 0; i < details.Count; i++)
            {
                var (label, value) = details[i];
                var border = i == details.Count - 1 ? "" : $"border-bottom:1px solid {Border};";
                rows.Append($"""
                    <tr>
                      <td valign="top" style="padding:12px 0 12px 18px;{border}width:120px;font-size:11.5px;font-weight:700;letter-spacing:.8px;text-transform:uppercase;color:{Muted}">{E(label)}</td>
                      <td valign="top" style="padding:12px 18px 12px 12px;{border}font-size:15px;font-weight:600;color:{Ink};word-break:break-word">{E(value)}</td>
                    </tr>
                    """);
            }
            return $"""<table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="margin:6px 0 22px;background:{Soft};border:1px solid {Border};border-radius:16px">{rows}</table>""";
        }

        private static string Quote(string text) => $"""
            <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="margin:6px 0 22px"><tr>
              <td style="width:4px;border-radius:4px;background-color:{Gold};background-image:linear-gradient(180deg,{Gold},{VioletBright})">&nbsp;</td>
              <td style="padding:18px 22px;background:{Soft};border-radius:0 14px 14px 0;font-size:15px;line-height:1.7;color:{Ink}">{E(text).ReplaceLineEndings("<br>")}</td>
            </tr></table>
            """;

        private static string Button(EmailButton button) => $"""
            <table role="presentation" cellpadding="0" cellspacing="0" align="center" class="mx-btn" style="margin:32px auto 0">
              <tr><td align="center" style="border-radius:14px;background-color:{Violet};background-image:linear-gradient(120deg,{VioletBright} 0%,#5b21b6 55%,{Violet} 100%);box-shadow:0 14px 26px -12px rgba(91,33,182,.85)">
                <a href="{E(button.Url)}" style="display:inline-block;padding:16px 34px;font-family:{Font};font-size:15.5px;font-weight:700;color:#ffffff;text-decoration:none;border-radius:14px">{E(button.Label)}&nbsp;&nbsp;&rarr;</a>
              </td></tr>
            </table>
            """;
    }
}
