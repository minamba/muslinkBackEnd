using System.Text;
using System.Text.RegularExpressions;
using static HelpMuslimWebApp.Mailing.EmailLayout;

namespace HelpMuslimWebApp.Mailing
{
    // Réglages d'un modèle de signature (onglet Administration › Boîte mail › Signatures)
    public record SignatureDesign(
        string? Closing,
        string? Tagline,
        bool ShowJobTitle,
        bool ShowEmail,
        bool ShowWebsite,
        string? Phone,
        string? Color);

    // Qui signe : l'administrateur qui écrit, au moment de l'envoi
    public record SignatureSigner(string? FullName, string? JobTitle, string? Email);

    // La signature mise en page : la formule, puis le logo, un trait violet → or, le nom (couleur du modèle),
    // la fonction, l'adresse, le téléphone et le site avec leurs icônes, un petit trait et la phrase en italique.
    // Tableaux, styles en ligne et images PNG jointes (cid:) : seule mise en page fiable dans les messageries.
    // Tout ce qui vient d'une saisie est échappé.
    public static partial class SignatureRenderer
    {
        public const string DefaultColor = "#4c1d95";

        // Modèle utilisé tant qu'aucun n'est enregistré
        public static readonly SignatureDesign Fallback = new("Cordialement,", "Trouvez l'opportunité qui vous correspond", true, true, true, null, DefaultColor);

        public static string SafeColor(string? color) => color != null && ColorRegex().IsMatch(color) ? color : DefaultColor;

        public static string Html(SignatureDesign design, SignatureSigner signer, string siteUrl)
        {
            string Img(string id) => EmailImages.Src(id);
            var name = string.IsNullOrWhiteSpace(signer.FullName) ? "L'équipe Muslink" : signer.FullName.Trim();
            var site = siteUrl.TrimEnd('/');
            var html = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(design.Closing))
                html.Append($"""<p style="margin:26px 0 0;font-family:{Font};font-size:15.5px;line-height:1.6;color:{Ink}">{E(design.Closing.Trim())}</p>""");

            html.Append($"""<table role="presentation" cellpadding="0" cellspacing="0" border="0" style="margin:14px 0 0"><tr>""");
            html.Append($"""<td style="padding:0 18px 0 0;vertical-align:middle"><img src="{Img(EmailImages.SignatureLogo)}" width="92" height="92" alt="Muslink" style="display:block;width:92px;height:92px;border:0"></td>""");
            html.Append($"""<td style="width:3px;vertical-align:middle"><img src="{Img(EmailImages.SignatureLine)}" width="3" height="132" alt="" style="display:block;width:3px;height:132px;border:0"></td>""");
            html.Append($"""<td style="padding:0 0 0 20px;vertical-align:middle;font-family:{Font}">""");
            html.Append($"""<div style="font-size:21px;line-height:1.2;font-weight:800;color:{SafeColor(design.Color)}">{E(name)}</div>""");

            if (design.ShowJobTitle && !string.IsNullOrWhiteSpace(signer.JobTitle))
                html.Append($"""<div style="margin-top:3px;font-size:15px;line-height:1.4;color:{Muted}">{E(signer.JobTitle.Trim())}</div>""");

            if (design.ShowEmail && !string.IsNullOrWhiteSpace(signer.Email))
                html.Append(IconLine(Img(EmailImages.IconMail), "✉", $"""<a href="mailto:{E(signer.Email)}" style="color:{Ink};text-decoration:none;white-space:nowrap">{E(signer.Email)}</a>""", 10));

            if (!string.IsNullOrWhiteSpace(design.Phone))
            {
                var phone = design.Phone.Trim();
                html.Append(IconLine(Img(EmailImages.IconPhone), "☎", $"""<a href="tel:{E(PhoneDigitsRegex().Replace(phone, string.Empty))}" style="color:{Ink};text-decoration:none">{E(phone)}</a>""", 4));
            }

            if (design.ShowWebsite)
                html.Append(IconLine(Img(EmailImages.IconWeb), "🌐", $"""<a href="{E(site)}" style="color:{Ink};text-decoration:none">www.muslink.fr</a>""", 4));

            if (!string.IsNullOrWhiteSpace(design.Tagline))
            {
                html.Append("""<div style="width:64px;height:2px;margin:10px 0 7px;background-color:#e2dcef;font-size:0;line-height:0">&nbsp;</div>""");
                html.Append($"""<div style="font-size:14px;line-height:1.45;font-style:italic;color:#5b5370">{E(design.Tagline.Trim())}</div>""");
            }

            html.Append("</td></tr></table>");
            return html.ToString();
        }

        public static string PlainText(SignatureDesign design, SignatureSigner signer)
        {
            var lines = new List<string>();
            if (!string.IsNullOrWhiteSpace(design.Closing)) lines.Add(design.Closing.Trim());
            lines.Add("--");
            lines.Add(string.IsNullOrWhiteSpace(signer.FullName) ? "L'équipe Muslink" : signer.FullName.Trim());
            if (design.ShowJobTitle && !string.IsNullOrWhiteSpace(signer.JobTitle)) lines.Add(signer.JobTitle.Trim());
            if (design.ShowEmail && !string.IsNullOrWhiteSpace(signer.Email)) lines.Add(signer.Email);
            if (!string.IsNullOrWhiteSpace(design.Phone)) lines.Add(design.Phone.Trim());
            if (design.ShowWebsite) lines.Add("www.muslink.fr");
            if (!string.IsNullOrWhiteSpace(design.Tagline)) lines.Add(design.Tagline.Trim());
            return string.Join('\n', lines);
        }

        // Une ligne « icône + texte », en tableau pour Outlook
        private static string IconLine(string image, string fallback, string content, int margin) =>
            $"""<table role="presentation" cellpadding="0" cellspacing="0" border="0" style="margin-top:{margin}px"><tr>"""
            + $"""<td style="padding:0 10px 0 0;vertical-align:middle"><img src="{image}" width="18" height="18" alt="{fallback}" style="display:block;width:18px;height:18px;border:0"></td>"""
            + $"""<td style="vertical-align:middle;font-size:14.5px;line-height:1.4;font-family:{Font}">{content}</td>"""
            + "</tr></table>";

        [GeneratedRegex("^#[0-9a-fA-F]{6}$")]
        private static partial Regex ColorRegex();
        [GeneratedRegex("[^0-9+]")]
        private static partial Regex PhoneDigitsRegex();
    }
}
