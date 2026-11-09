using System.Text.RegularExpressions;
using HelpMuslimWebApp.Api.Services;

namespace HelpMuslimWebApp.Api.Utils
{
    // Fichiers d'un article (images, vidéos, documents, couverture) retrouvés dans son contenu
    public static class ArticleMedia
    {
        private static readonly Regex MediaPaths = new(@"/uploads/articles/(images|videos|documents)/[\w.\-]+", RegexOptions.Compiled);

        public static readonly IReadOnlyList<FileCategory> Categories = [FileCategory.ArticleImage, FileCategory.ArticleVideo, FileCategory.ArticleDocument];

        // Chemins stockés (« /uploads/articles/images/xxx.jpg ») cités dans un ou plusieurs textes
        public static HashSet<string> Paths(params string?[] texts) =>
            texts.Where(t => !string.IsNullOrEmpty(t)).SelectMany(t => MediaPaths.Matches(t!).Select(m => m.Value)).ToHashSet();

        public static FileCategory CategoryOf(string path) =>
            path.Contains("/videos/") ? FileCategory.ArticleVideo
            : path.Contains("/documents/") ? FileCategory.ArticleDocument
            : FileCategory.ArticleImage;
    }
}
