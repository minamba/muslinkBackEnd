using MimeKit;

namespace HelpMuslimWebApp.Mailing
{
    // Logos des emails, embarqués dans l'assembly et joints en pièces liées (cid:).
    // Une image en ligne ne s'afficherait pas depuis un poste de développement (localhost injoignable
    // pour Gmail) et serait bloquée par défaut par de nombreux clients mail ; une pièce liée s'affiche partout.
    public static class EmailImages
    {
        public const string HeaderLogo = "muslink-logo";
        public const string SignatureLogo = "muslink-signature";
        public const string SignatureLine = "muslink-trait";
        public const string IconMail = "muslink-icon-mail";
        public const string IconPhone = "muslink-icon-tel";
        public const string IconWeb = "muslink-icon-web";

        private static readonly Dictionary<string, string> Resources = new()
        {
            [HeaderLogo] = "logo-header.png",
            [SignatureLogo] = "logo-signature.png",
            [SignatureLine] = "signature-trait.png",
            [IconMail] = "signature-mail.png",
            [IconPhone] = "signature-tel.png",
            [IconWeb] = "signature-web.png",
        };

        public static string Src(string contentId) => $"cid:{contentId}";

        // Joint au message les logos que son HTML utilise (adresse exacte, guillemets compris : un identifiant
        // ne doit jamais en attraper un autre qui commencerait pareil)
        public static void Attach(BodyBuilder builder, string html)
        {
            foreach (var (contentId, file) in Resources)
            {
                if (!html.Contains($"\"{Src(contentId)}\"", StringComparison.Ordinal)) continue;
                var image = builder.LinkedResources.Add(file, Read(file), new ContentType("image", "png"));
                image.ContentId = contentId;
            }
        }

        // Pour un affichage dans le navigateur (aperçu, copie locale) : les cid: remplacés par les images elles-mêmes
        public static string ToDataUris(string html)
        {
            foreach (var (contentId, file) in Resources)
            {
                var quoted = $"\"{Src(contentId)}\"";
                if (html.Contains(quoted, StringComparison.Ordinal))
                    html = html.Replace(quoted, $"\"data:image/png;base64,{Convert.ToBase64String(Read(file))}\"");
            }
            return html;
        }

        private static byte[] Read(string file)
        {
            using var stream = typeof(EmailImages).Assembly.GetManifestResourceStream($"HelpMuslimWebApp.Mailing.Resources.{file}")
                ?? throw new FileNotFoundException($"Image d'email introuvable : {file}");
            using var memory = new MemoryStream();
            stream.CopyTo(memory);
            return memory.ToArray();
        }
    }
}
