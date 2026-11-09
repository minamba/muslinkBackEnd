namespace HelpMuslimWebApp.Api.Services
{
    // Dossier des fichiers déposés (photos, logos, CV, médias d'articles), hors du dossier de l'Api :
    // un déploiement ne peut pas les effacer. Réglage « Storage:Root » (chemin absolu, ou relatif au dossier de l'Api).
    //   {Root}/public  : servi tel quel sous /uploads (photos, logos, médias d'articles)
    //   {Root}/private : jamais servi directement (CV, téléchargés via l'Api après contrôle des droits)
    //   {Root}/demo/public, {Root}/demo/private : fichiers de la démo (servis eux aussi sous /uploads)
    public class StorageLocation
    {
        public string Root { get; }
        public string PublicRoot { get; }
        public string PrivateRoot { get; }
        public string DemoPublicRoot { get; }
        public string DemoPrivateRoot { get; }

        public StorageLocation(IConfiguration configuration, IWebHostEnvironment env)
        {
            var root = configuration["Storage:Root"];
            if (string.IsNullOrWhiteSpace(root))
                throw new InvalidOperationException("Réglage « Storage:Root » manquant : dossier des fichiers déposés (photos, CV…), à placer hors du dossier de l'Api.");

            root = Path.GetFullPath(Path.Combine(env.ContentRootPath, root));
            Root = root;
            PublicRoot = Path.Combine(root, "public");
            PrivateRoot = Path.Combine(root, "private");
            DemoPublicRoot = Path.Combine(root, "demo", "public");
            DemoPrivateRoot = Path.Combine(root, "demo", "private");
            foreach (var directory in new[] { PublicRoot, PrivateRoot, DemoPublicRoot, DemoPrivateRoot }) Directory.CreateDirectory(directory);
        }

        // Fichiers des versions précédentes (wwwroot/uploads, App_Data/uploads) déplacés vers ce dossier ;
        // un fichier déjà présent à l'arrivée n'est pas écrasé
        public int MoveLegacyFiles(IWebHostEnvironment env) =>
            Move(Path.Combine(env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot"), "uploads"), PublicRoot)
            + Move(Path.Combine(env.ContentRootPath, "App_Data", "uploads"), PrivateRoot);

        private static int Move(string source, string destination)
        {
            if (!Directory.Exists(source)) return 0;

            var moved = 0;
            foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
            {
                var target = Path.Combine(destination, Path.GetRelativePath(source, file));
                if (File.Exists(target)) continue;
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                File.Move(file, target);
                moved++;
            }
            return moved;
        }
    }
}
