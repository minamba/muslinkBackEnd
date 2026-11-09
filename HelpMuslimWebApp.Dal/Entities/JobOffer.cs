namespace HelpMuslimWebApp.Dal.Entities
{
    public partial class JobOffer
    {
        public int Id { get; set; }
        public int? RecruiterProfileId { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public int? DomainId { get; set; }
        public int? ProfessionId { get; set; }
        public string? ContractType { get; set; }
        public string? City { get; set; }
        public bool? IsRemote { get; set; }
        // Années d'expérience demandées dans le domaine (0 : débutant accepté) ; vide : non précisé
        public int? ExperienceYears { get; set; }
        public string? Salary { get; set; }
        public string? Duration { get; set; }
        public DateOnly? StartDate { get; set; }
        public bool? IsOpen { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
        public bool? IsFilled { get; set; }
        public DateTime? FilledAt { get; set; }
        public DateTime? InDiscussionAt { get; set; }
        public bool? IsHidden { get; set; }

        // Offre sponsorisée jusqu'à… : en tête des listes d'offres, avec un badge « Sponsorisée »
        public DateTime? SponsoredUntil { get; set; }
        // Statistiques : consultations de l'offre (hors recruteur et administration)
        public int ViewsCount { get; set; }

        public virtual RecruiterProfile? RecruiterProfile { get; set; }
        public virtual JobDomain? Domain { get; set; }
        public virtual Profession? Profession { get; set; }
        public virtual ICollection<JobApplication> Applications { get; set; } = new List<JobApplication>();
        public virtual ICollection<JobOfferSkill> Skills { get; set; } = new List<JobOfferSkill>();
    }
}
