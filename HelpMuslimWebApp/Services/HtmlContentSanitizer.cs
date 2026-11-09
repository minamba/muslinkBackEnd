using AngleSharp.Html.Dom;
using Ganss.Xss;

namespace HelpMuslimWebApp.Api.Services
{
    // Liste blanche stricte : uniquement ce que l'éditeur sait produire.
    // Pas de script, pas de style inline ; iframes limitées à YouTube / Vimeo.
    public class HtmlContentSanitizer : IHtmlContentSanitizer
    {
        private static readonly string[] Tags =
        [
            "p", "br", "h2", "h3", "h4", "strong", "b", "em", "i", "u", "s", "mark", "a", "ul", "ol", "li",
            "blockquote", "hr", "img", "video", "iframe", "div", "span", "code", "pre", "figure", "figcaption",
        ];

        private static readonly string[] Attributes =
        [
            "href", "target", "rel", "title", "src", "alt", "controls", "preload", "width", "height",
            "allow", "allowfullscreen", "frameborder", "start", "class",
        ];

        private static readonly string[] EmbedHosts = ["www.youtube.com", "youtube.com", "www.youtube-nocookie.com", "player.vimeo.com"];

        private readonly HtmlSanitizer _sanitizer;

        public HtmlContentSanitizer()
        {
            _sanitizer = new HtmlSanitizer();
            _sanitizer.AllowedTags.Clear();
            _sanitizer.AllowedTags.UnionWith(Tags);
            _sanitizer.AllowedAttributes.Clear();
            _sanitizer.AllowedAttributes.UnionWith(Attributes);
            _sanitizer.AllowedCssProperties.Clear();
            _sanitizer.AllowedSchemes.Add("mailto");
            _sanitizer.AllowDataAttributes = true;

            // Vidéos intégrées : uniquement depuis les plateformes autorisées
            _sanitizer.FilterUrl += (_, e) =>
            {
                if (e.Tag is IHtmlInlineFrameElement && !IsAllowedEmbed(e.OriginalUrl)) e.SanitizedUrl = null;
            };

            // Liens externes ouverts dans un nouvel onglet sans accès à la page d'origine
            _sanitizer.PostProcessNode += (_, e) =>
            {
                if (e.Node is IHtmlAnchorElement link && link.GetAttribute("target") == "_blank")
                    link.SetAttribute("rel", "noopener noreferrer nofollow");
            };
        }

        public string Sanitize(string html) => _sanitizer.Sanitize(html ?? string.Empty);

        private static bool IsAllowedEmbed(string? url) =>
            Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps && EmbedHosts.Contains(uri.Host);
    }
}
