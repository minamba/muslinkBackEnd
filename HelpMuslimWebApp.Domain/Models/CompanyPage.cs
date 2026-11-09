namespace HelpMuslimWebApp.Domain.Models
{
    // Page entreprise enrichie : remplie depuis « Mon entreprise », affichée sur la fiche publique de l'entreprise
    public class CompanyPage
    {
        public string? CoverPath { get; set; }
        public List<string> Benefits { get; set; } = [];
        // « Bon à savoir » : ambiance, valeurs, façon de travailler
        public string? Culture { get; set; }
        // Les profils recherchés
        public string? LookingFor { get; set; }
        public List<string> RecruitmentSteps { get; set; } = [];
        public int? FoundedYear { get; set; }
        public string? LinkedinUrl { get; set; }
        public string? InstagramUrl { get; set; }
        public string? FacebookUrl { get; set; }
        public string? YoutubeUrl { get; set; }
        public List<CompanyPhoto> Photos { get; set; } = [];
        public List<CompanyVideo> Videos { get; set; } = [];
    }

    public class CompanyPhoto
    {
        public int Id { get; set; }
        public string Path { get; set; } = string.Empty;
        public string? Caption { get; set; }
    }

    // Vidéo hébergée sur YouTube ou Vimeo : la page n'affiche que sa miniature et l'ouvre dans un lecteur intégré
    public class CompanyVideo
    {
        public int Id { get; set; }
        public string Provider { get; set; } = VideoProviders.Youtube;
        public string VideoId { get; set; } = string.Empty;
        public string? ThumbnailUrl { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Quote { get; set; }
    }

    public static class VideoProviders
    {
        public const string Youtube = "youtube";
        public const string Vimeo = "vimeo";
    }

    public static class CompanyMediaKinds
    {
        public const string Photo = "photo";
        public const string Video = "video";

        // Limites par entreprise
        public const int MaxPhotos = 12;
        public const int MaxVideos = 6;
    }
}
