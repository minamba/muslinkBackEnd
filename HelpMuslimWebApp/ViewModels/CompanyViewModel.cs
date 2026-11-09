namespace HelpMuslimWebApp.Api.ViewModels
{
    // Entreprise de l'annuaire (vue publique d'un profil recruteur, sans données personnelles)
    public class CompanyViewModel
    {
        public int Id { get; set; }
        public string? CompanyName { get; set; }
        public string? LogoUrl { get; set; }
        public string? CompanySector { get; set; }
        public string? CompanySize { get; set; }
        public string? City { get; set; }
        public string? CompanyWebsite { get; set; }
        public string? CompanyDescription { get; set; }
        public int OpenOffersCount { get; set; }

        // Visible uniquement du recruteur lui-même et de l'administration (sinon l'entreprise est introuvable)
        public bool IsDisabled { get; set; }

        // Entreprise fondatrice (badge)
        public bool IsFounder { get; set; }
    }

    // Administration › Entreprises : recruteur rattaché, nombre d'offres, état
    public class CompanyAdminViewModel : CompanyViewModel
    {
        public string? RecruiterName { get; set; }
        public string? RecruiterEmail { get; set; }
        public int OffersCount { get; set; }
        public DateTime? DisabledAt { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    // Activité de l'annuaire (liste déroulante du filtre) et nombre d'entreprises
    public class CompanySectorViewModel
    {
        public string Sector { get; set; } = "";
        public int Count { get; set; }
    }

    // Page entreprise : présentation + offres ouvertes et visibles
    public class CompanyDetailViewModel : CompanyViewModel
    {
        public List<JobOfferViewModel> Offers { get; set; } = [];
        public CompanyPageViewModel Page { get; set; } = new();
        // Page du recruteur connecté (pas de « Signaler »)
        public bool IsOwner { get; set; }
    }

    // Page entreprise enrichie : couverture, avantages, ambiance, profils recherchés, recrutement, réseaux, photos et vidéos
    public class CompanyPageViewModel
    {
        public string? CoverUrl { get; set; }
        public List<string> Benefits { get; set; } = [];
        public string? Culture { get; set; }
        public string? LookingFor { get; set; }
        public List<string> RecruitmentSteps { get; set; } = [];
        public int? FoundedYear { get; set; }
        public string? LinkedinUrl { get; set; }
        public string? InstagramUrl { get; set; }
        public string? FacebookUrl { get; set; }
        public string? YoutubeUrl { get; set; }
        public List<CompanyPhotoViewModel> Photos { get; set; } = [];
        public List<CompanyVideoViewModel> Videos { get; set; } = [];
        // Limites (formulaire du recruteur)
        public int MaxPhotos { get; set; }
        public int MaxVideos { get; set; }
    }

    public class CompanyPhotoViewModel
    {
        public int Id { get; set; }
        public string Url { get; set; } = string.Empty;
        public string? Caption { get; set; }
    }

    public class CompanyVideoViewModel
    {
        public int Id { get; set; }
        // « youtube » ou « vimeo »
        public string Provider { get; set; } = string.Empty;
        public string VideoId { get; set; } = string.Empty;
        public string? ThumbnailUrl { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Quote { get; set; }
    }

    // Alerte temps réel : une entreprise suivie vient de publier une offre
    public class CompanyOfferNotificationViewModel
    {
        public int OfferId { get; set; }
        public string? Title { get; set; }
        public string? City { get; set; }
        public string? ContractType { get; set; }
        public int CompanyId { get; set; }
        public string? CompanyName { get; set; }
        public string? CompanyLogoUrl { get; set; }
    }
}
