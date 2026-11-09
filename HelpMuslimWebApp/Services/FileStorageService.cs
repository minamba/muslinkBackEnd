using HelpMuslimWebApp.Api.Services.Demo;

namespace HelpMuslimWebApp.Api.Services
{
    public class FileStorageService : IFileStorageService
    {
        private static readonly Dictionary<FileCategory, (string Folder, string[] Extensions, long MaxBytes, bool IsPublic)> Rules = new()
        {
            [FileCategory.Photo] = ("photos", [".jpg", ".jpeg", ".png", ".webp"], 2 * 1024 * 1024, true),
            [FileCategory.Logo] = ("logos", [".jpg", ".jpeg", ".png", ".webp"], 2 * 1024 * 1024, true),
            [FileCategory.Cv] = ("cvs", [".pdf", ".doc", ".docx"], 5 * 1024 * 1024, false),
            [FileCategory.ArticleImage] = ("articles/images", [".jpg", ".jpeg", ".png", ".webp", ".gif"], 8 * 1024 * 1024, true),
            [FileCategory.ArticleVideo] = ("articles/videos", [".mp4", ".webm"], 100 * 1024 * 1024, true),
            [FileCategory.CompanyCover] = ("companies/covers", [".jpg", ".jpeg", ".png", ".webp"], 5 * 1024 * 1024, true),
            [FileCategory.CompanyPhoto] = ("companies/photos", [".jpg", ".jpeg", ".png", ".webp"], 5 * 1024 * 1024, true),
            [FileCategory.CompanyThumbnail] = ("companies/thumbnails", [".jpg", ".jpeg", ".png", ".webp"], 2 * 1024 * 1024, true),
            [FileCategory.ArticleDocument] = ("articles/documents", [".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".odt", ".txt"], 20 * 1024 * 1024, true),
        };

        // Catégorie d'un média d'article selon son extension (null = format non pris en charge)
        public static FileCategory? ArticleCategoryOf(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            foreach (var category in new[] { FileCategory.ArticleImage, FileCategory.ArticleVideo, FileCategory.ArticleDocument })
            {
                if (Rules[category].Extensions.Contains(extension)) return category;
            }
            return null;
        }

        private static readonly Dictionary<string, string> ContentTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".png"] = "image/png",
            [".webp"] = "image/webp",
            [".gif"] = "image/gif",
            [".mp4"] = "video/mp4",
            [".webm"] = "video/webm",
            [".xls"] = "application/vnd.ms-excel",
            [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            [".ppt"] = "application/vnd.ms-powerpoint",
            [".pptx"] = "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            [".odt"] = "application/vnd.oasis.opendocument.text",
            [".txt"] = "text/plain",
            [".pdf"] = "application/pdf",
            [".doc"] = "application/msword",
            [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        };

        private readonly StorageLocation _location;
        private readonly IDemoContext _demo;

        public FileStorageService(StorageLocation location, IDemoContext demo)
        {
            _location = location;
            _demo = demo;
        }

        // Requête de démo : fichiers de la démo, jamais les vrais
        private string _publicRoot => _demo.IsDemo ? _location.DemoPublicRoot : _location.PublicRoot;
        private string _privateRoot => _demo.IsDemo ? _location.DemoPrivateRoot : _location.PrivateRoot;

        public async Task<string> SaveAsync(IFormFile file, FileCategory category)
        {
            var rule = Rules[category];
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (file.Length == 0)
                throw new ArgumentException("Le fichier est vide.");
            if (!rule.Extensions.Contains(extension))
                throw new ArgumentException($"Format non autorisé. Formats acceptés : {string.Join(", ", rule.Extensions)}.");
            if (file.Length > rule.MaxBytes)
                throw new ArgumentException($"Le fichier dépasse la taille maximale de {rule.MaxBytes / (1024 * 1024)} Mo.");

            // Nom aléatoire : jamais le nom fourni par l'utilisateur sur le disque
            var fileName = $"{Guid.NewGuid():N}{extension}";
            var directory = Path.Combine(rule.IsPublic ? _publicRoot : _privateRoot, rule.Folder);
            Directory.CreateDirectory(directory);

            await using (var stream = File.Create(Path.Combine(directory, fileName)))
            {
                await file.CopyToAsync(stream);
            }

            return rule.IsPublic ? $"/uploads/{rule.Folder}/{fileName}" : $"{rule.Folder}/{fileName}";
        }

        public async Task<string> SaveAsync(byte[] content, string extension, FileCategory category)
        {
            var rule = Rules[category];
            extension = extension.ToLowerInvariant();
            if (content.Length == 0) throw new ArgumentException("Le fichier est vide.");
            if (!rule.Extensions.Contains(extension)) throw new ArgumentException("Format non autorisé.");
            if (content.Length > rule.MaxBytes) throw new ArgumentException($"Le fichier dépasse la taille maximale de {rule.MaxBytes / (1024 * 1024)} Mo.");

            var fileName = $"{Guid.NewGuid():N}{extension}";
            var directory = Path.Combine(rule.IsPublic ? _publicRoot : _privateRoot, rule.Folder);
            Directory.CreateDirectory(directory);
            await File.WriteAllBytesAsync(Path.Combine(directory, fileName), content);
            return rule.IsPublic ? $"/uploads/{rule.Folder}/{fileName}" : $"{rule.Folder}/{fileName}";
        }

        // Fichier d'une page entreprise d'après son dossier : couverture, photo ou miniature de vidéo
        public static FileCategory CompanyCategoryOf(string storedPath) =>
            storedPath.Contains("/companies/covers/") ? FileCategory.CompanyCover
            : storedPath.Contains("/companies/thumbnails/") ? FileCategory.CompanyThumbnail
            : FileCategory.CompanyPhoto;

        public string? Copy(string? storedPath, FileCategory category)
        {
            var source = Resolve(storedPath, category);
            if (source == null || !File.Exists(source)) return null;

            var rule = Rules[category];
            var fileName = $"{Guid.NewGuid():N}{Path.GetExtension(source).ToLowerInvariant()}";
            File.Copy(source, Path.Combine(Path.GetDirectoryName(source)!, fileName));
            return rule.IsPublic ? $"/uploads/{rule.Folder}/{fileName}" : $"{rule.Folder}/{fileName}";
        }

        public void Delete(string? storedPath, FileCategory category)
        {
            var fullPath = Resolve(storedPath, category);
            if (fullPath != null && File.Exists(fullPath)) File.Delete(fullPath);
        }

        public IEnumerable<(string StoredPath, DateTime LastWriteUtc)> List(FileCategory category)
        {
            var rule = Rules[category];
            var directory = Path.Combine(rule.IsPublic ? _publicRoot : _privateRoot, rule.Folder);
            if (!Directory.Exists(directory)) return [];
            return new DirectoryInfo(directory).EnumerateFiles()
                .Select(f => (rule.IsPublic ? $"/uploads/{rule.Folder}/{f.Name}" : $"{rule.Folder}/{f.Name}", f.LastWriteTimeUtc))
                .ToList();
        }

        public Stream? OpenRead(string storedPath, FileCategory category)
        {
            var fullPath = Resolve(storedPath, category);
            return fullPath != null && File.Exists(fullPath) ? File.OpenRead(fullPath) : null;
        }

        public string GetContentType(string fileName) =>
            ContentTypes.TryGetValue(Path.GetExtension(fileName), out var type) ? type : "application/octet-stream";

        // Résout le chemin sur disque en interdisant toute sortie du dossier de la catégorie
        private string? Resolve(string? storedPath, FileCategory category)
        {
            if (string.IsNullOrWhiteSpace(storedPath)) return null;

            var rule = Rules[category];
            var directory = Path.GetFullPath(Path.Combine(rule.IsPublic ? _publicRoot : _privateRoot, rule.Folder));
            var fullPath = Path.GetFullPath(Path.Combine(directory, Path.GetFileName(storedPath)));
            return fullPath.StartsWith(directory, StringComparison.OrdinalIgnoreCase) ? fullPath : null;
        }
    }
}
