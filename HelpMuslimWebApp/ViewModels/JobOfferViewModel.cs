namespace HelpMuslimWebApp.Api.ViewModels
{
    public class JobOfferViewModel
    {
        public int Id { get; set; }
        public int? RecruiterProfileId { get; set; }
        public string? CompanyName { get; set; }
        public string? CompanyLogoUrl { get; set; }
        public string? RecruiterFirstName { get; set; }
        public string? RecruiterLastName { get; set; }
        public string? RecruiterJobTitle { get; set; }
        public string? RecruiterPhone { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public int? DomainId { get; set; }
        public string? DomainName { get; set; }
        public int? ProfessionId { get; set; }
        public string? ProfessionName { get; set; }
        public string? ContractType { get; set; }
        public string? City { get; set; }
        public bool? IsRemote { get; set; }
        // Années d'expérience demandées dans le domaine (0 : débutant accepté) ; vide : non précisé
        public int? ExperienceYears { get; set; }
        public string? Salary { get; set; }
        public string? Duration { get; set; }
        public DateOnly? StartDate { get; set; }
        public List<string> RequiredSkills { get; set; } = [];
        public List<string> BonusSkills { get; set; } = [];
        public bool? IsOpen { get; set; }
        public string? Status { get; set; }
        public int ApplicationsCount { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
        public bool IsFilled { get; set; }
        public bool IsHidden { get; set; }
        public bool CompanyDisabled { get; set; }

        // Entreprise fondatrice (badge)
        public bool CompanyIsFounder { get; set; }

        // Offre sponsorisée (badge « Sponsorisée », en tête des listes) et date de fin du parrainage
        public bool IsSponsored { get; set; }
        public DateTime? SponsoredUntil { get; set; }

        // Statistiques (affichées au recruteur propriétaire)
        public int ViewsCount { get; set; }
        public DateTime? FilledAt { get; set; }

        // Selon l'utilisateur connecté
        public bool IsOwner { get; set; }
        public bool HasApplied { get; set; }

        // Candidat connecté : correspondance avec son profil (0 à 100)
        public int? MatchScore { get; set; }
        public List<string>? MatchReasons { get; set; }

        // Compétences de l'offre (requises et bonus) que le candidat possède
        public List<string>? MatchedSkills { get; set; }

        // Offres selon les vœux du candidat (« Ce que je cherche ») : score et critères évalués
        public int? WishScore { get; set; }
        public List<WishCriterionViewModel>? WishCriteria { get; set; }

        // Candidat gratuit (Premium candidats activé) : offre verrouillée, seul le score est renseigné
        public bool IsLocked { get; set; }
    }

    // Status : "ok" (rempli), "partial" (en partie), "ko" (non rempli)
    public class WishCriterionViewModel
    {
        public string Label { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    // Offres qui correspondent aux vœux du candidat (au moins 70 %), de la plus proche à la plus éloignée
    public class WishOffersViewModel
    {
        // Le candidat a renseigné au moins un critère qui départage les offres
        public bool HasWishes { get; set; }
        public int Threshold { get; set; }
        public int Total { get; set; }
        // Premium candidats activé et candidat gratuit : aperçu d'une offre, les autres verrouillées
        public bool PremiumRequired { get; set; }
        public List<JobOfferViewModel> Items { get; set; } = [];
    }
}
