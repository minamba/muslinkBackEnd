using System.Text.RegularExpressions;
using AngleSharp.Html.Dom;
using Ganss.Xss;
using MimeKit;

namespace HelpMuslimWebApp.Api.Services.Mail
{
    // HTML d'un email reçu, rendu affichable dans l'administration.
    // Première barrière : liste blanche (pas de script, formulaire, iframe, attribut d'événement, lien javascript:).
    // Seconde barrière côté front : affichage dans une iframe sandbox sans droit d'exécution.
    public static partial class MailHtmlSanitizer
    {
        private const long MaxInlineImagesBytes = 10 * 1024 * 1024;

        // showImages = false : les images et fonds distants (pixels espions) sont retirés, imagesBlocked le signale
        public static string Sanitize(string html, MimeMessage message, bool showImages, out bool imagesBlocked)
        {
            var blocked = false;
            var sanitizer = new HtmlSanitizer();
            sanitizer.AllowedTags.Add("style");
            sanitizer.AllowedTags.Add("center");
            sanitizer.AllowedTags.Add("font");
            sanitizer.AllowedAttributes.Add("class");
            sanitizer.AllowedAttributes.Add("bgcolor");
            sanitizer.AllowedAttributes.Add("color");
            sanitizer.AllowedAttributes.Add("face");
            sanitizer.AllowedSchemes.Add("mailto");
            sanitizer.AllowedSchemes.Add("tel");
            sanitizer.AllowedSchemes.Add("cid");

            sanitizer.FilterUrl += (_, e) =>
            {
                // Les liens restent ; toute autre ressource distante attend « Afficher les images »
                if (showImages || e.Tag is IHtmlAnchorElement) return;
                if (e.OriginalUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase) || e.OriginalUrl.StartsWith("//"))
                {
                    e.SanitizedUrl = null;
                    blocked = true;
                }
            };

            // Liens ouverts dans un nouvel onglet, sans accès à l'administration
            sanitizer.PostProcessNode += (_, e) =>
            {
                if (e.Node is IHtmlAnchorElement link)
                {
                    link.SetAttribute("target", "_blank");
                    link.SetAttribute("rel", "noopener noreferrer nofollow");
                }
            };

            var clean = sanitizer.Sanitize(html);
            imagesBlocked = blocked;
            return EmbedInlineImages(clean, message);
        }

        // Images en pièce liée (cid:) : incorporées en base64, le navigateur ne sait pas résoudre cid:.
        // Celles que le message ne contient pas (logo d'un email cité) sont retirées plutôt que laissées cassées.
        private static string EmbedInlineImages(string html, MimeMessage message)
        {
            var total = 0L;
            foreach (var part in message.BodyParts.OfType<MimePart>())
            {
                if (string.IsNullOrEmpty(part.ContentId) || part.ContentType?.MediaType != "image") continue;
                if (!html.Contains($"cid:{part.ContentId}", StringComparison.OrdinalIgnoreCase)) continue;

                using var memory = new MemoryStream();
                part.Content!.DecodeTo(memory);
                total += memory.Length;
                if (total > MaxInlineImagesBytes) break;

                html = Regex.Replace(html, $"cid:{Regex.Escape(part.ContentId)}",
                    $"data:{part.ContentType!.MimeType};base64,{Convert.ToBase64String(memory.ToArray())}", RegexOptions.IgnoreCase);
            }
            return UnresolvedCidImageRegex().Replace(html, string.Empty);
        }

        [GeneratedRegex(@"<img\b[^>]*\ssrc\s*=\s*(""|')\s*cid:[^""']*(""|')[^>]*>", RegexOptions.IgnoreCase)]
        private static partial Regex UnresolvedCidImageRegex();
    }
}
