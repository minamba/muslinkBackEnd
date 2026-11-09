using System.Text.RegularExpressions;
using HelpMuslimWebApp.Domain.Models;

namespace HelpMuslimWebApp.Api.Utils
{
    // Lien de vidéo collé par le recruteur → plateforme et identifiant.
    // YouTube : youtube.com/watch?v=…, youtu.be/…, /shorts/…, /embed/…, /live/…, m.youtube.com, youtube-nocookie.com (11 caractères).
    // Vimeo : vimeo.com/123456, vimeo.com/123456/clé (non répertoriée), player.vimeo.com/video/123456?h=clé,
    //         vimeo.com/channels/…/123456, vimeo.com/showcase/…/video/123456. Id : « 123456 » ou « 123456/clé ».
    // PageUrl : adresse de la vidéo, donnée au service oEmbed de Vimeo pour sa miniature
    public record VideoLink(string Provider, string Id, string PageUrl)
    {
        private static readonly Regex YoutubeId = new("^[A-Za-z0-9_-]{11}$", RegexOptions.Compiled);
        private static readonly Regex VimeoId = new("^[0-9]{6,12}$", RegexOptions.Compiled);
        private static readonly Regex VimeoHash = new("^[0-9a-f]{6,20}$", RegexOptions.Compiled);

        public static VideoLink? Parse(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;
            var text = url.Trim();
            if (!text.Contains("://")) text = "https://" + text;
            if (!Uri.TryCreate(text, UriKind.Absolute, out var uri) || (uri.Scheme != "https" && uri.Scheme != "http")) return null;

            var host = uri.Host.ToLowerInvariant();
            if (host.StartsWith("www.")) host = host[4..];
            if (host.StartsWith("m.")) host = host[2..];
            var segments = uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
            var query = System.Web.HttpUtility.ParseQueryString(uri.Query);

            switch (host)
            {
                case "youtu.be":
                case "youtube.com":
                case "youtube-nocookie.com":
                    var youtube = host == "youtu.be" ? segments.FirstOrDefault()
                        : segments is ["watch", ..] ? query["v"]
                        : segments is ["shorts" or "embed" or "live" or "v", var id, ..] ? id
                        : null;
                    return youtube != null && YoutubeId.IsMatch(youtube)
                        ? new VideoLink(VideoProviders.Youtube, youtube, $"https://www.youtube.com/watch?v={youtube}")
                        : null;

                case "vimeo.com":
                case "player.vimeo.com":
                    // Le numéro de la vidéo, puis éventuellement la clé d'une vidéo non répertoriée (segment suivant ou ?h=)
                    var index = Array.FindIndex(segments, s => VimeoId.IsMatch(s));
                    if (index < 0) return null;
                    var number = segments[index];
                    var hash = index + 1 < segments.Length && VimeoHash.IsMatch(segments[index + 1]) ? segments[index + 1] : query["h"];
                    if (hash != null && !VimeoHash.IsMatch(hash)) hash = null;
                    return new VideoLink(VideoProviders.Vimeo, hash == null ? number : $"{number}/{hash}",
                        hash == null ? $"https://vimeo.com/{number}" : $"https://vimeo.com/{number}/{hash}");

                default:
                    return null;
            }
        }
    }
}
