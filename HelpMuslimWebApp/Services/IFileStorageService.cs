namespace HelpMuslimWebApp.Api.Services
{
    public enum FileCategory
    {
        // Dossiers sous Storage:Root (voir StorageLocation)
        Photo,  // public : public/photos
        Logo,   // public : public/logos
        Cv,     // privé  : private/cvs (servi via l'Api, accès contrôlé)
        ArticleImage,     // public : public/articles/images
        ArticleVideo,     // public : public/articles/videos
        ArticleDocument,  // public : public/articles/documents
        CompanyCover,     // public : public/companies/covers (photo de couverture de la page entreprise)
        CompanyPhoto,     // public : public/companies/photos (galerie de la page entreprise)
        CompanyThumbnail, // public : public/companies/thumbnails (miniatures des vidéos YouTube / Vimeo, copiées à l'ajout)
    }

    public interface IFileStorageService
    {
        // Retourne le chemin stocké en base (URL relative pour les fichiers publics)
        Task<string> SaveAsync(IFormFile file, FileCategory category);
        // Fichier téléchargé par l'Api (ex. miniature d'une vidéo) : mêmes règles de format et de taille
        Task<string> SaveAsync(byte[] content, string extension, FileCategory category);
        // Copie d'un fichier déjà stocké (même catégorie, nouveau nom) ; null si le fichier n'existe plus
        string? Copy(string? storedPath, FileCategory category);
        void Delete(string? storedPath, FileCategory category);
        // Fichiers d'une catégorie (chemin stocké en base, date de dernière écriture UTC) : nettoyage des fichiers orphelins
        IEnumerable<(string StoredPath, DateTime LastWriteUtc)> List(FileCategory category);
        Stream? OpenRead(string storedPath, FileCategory category);
        string GetContentType(string fileName);
    }
}
