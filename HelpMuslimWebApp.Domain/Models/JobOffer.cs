namespace HelpMuslimWebApp.Domain.Models
{
    public class JobOffer
    {
        public int Id { get; set; }
        public int? RecruiterProfileId { get; set; }
        public string? RecruiterUserId { get; set; }
        public string? CompanyName { get; set; }
        public string? CompanyLogoPath { get; set; }

        // Interlocuteur affiché sur l'offre ; téléphone seulement si le recruteur a choisi de l'afficher
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

        // Compétences attendues : requises (comptent le plus dans la correspondance) et bonus
        public List<string> RequiredSkills { get; set; } = [];
        public List<string> BonusSkills { get; set; } = [];

        public bool? IsOpen { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? ClosedAt { get; set; }

        // Fermée parce que le recruteur a trouvé son candidat
        public bool? IsFilled { get; set; }
        public DateTime? FilledAt { get; set; }

        // Le recruteur a écrit à un candidat qui a postulé à cette offre
        public DateTime? InDiscussionAt { get; set; }

        // Masquée par le recruteur : invisible du public (fil, accueil, pages entreprise)
        public bool? IsHidden { get; set; }

        // Entreprise désactivée par l'administration : l'offre est cachée du public comme si elle était masquée
        public bool CompanyDisabled { get; set; }

        // Entreprise fondatrice (badge)
        public bool CompanyIsFounder { get; set; }

        // Recruteur marqué compte de test : offre jamais signalée à Google (sitemap, données structurées)
        public bool RecruiterIsTestAccount { get; set; }

        // Sponsorisée jusqu'à… : en tête des listes d'offres avec un badge « Sponsorisée » (payée, donc honorée
        // jusqu'au bout même si le premium recruteurs est coupé entre-temps)
        public DateTime? SponsoredUntil { get; set; }
        public bool IsSponsored => SponsoredUntil > DateTime.UtcNow;
        public int ViewsCount { get; set; }
        public int ApplicationsCount { get; set; }

        public bool IsPubliclyHidden => IsHidden == true || CompanyDisabled;

        public string Status => OfferStatuses.Compute(IsOpen, CreatedAt, InDiscussionAt, IsFilled);
    }
}
