namespace HelpMuslimWebApp.Domain.Models
{
    public class OfferFilter
    {
        public int? DomainId { get; set; }
        public int? ProfessionId { get; set; }
        public string? ContractType { get; set; }
        public string? Search { get; set; }
        public string? City { get; set; }
        public int? RecruiterProfileId { get; set; }

        // false = inclure les offres fermées (recruteur propriétaire, admin)
        public bool OnlyOpen { get; set; } = true;

        // Listes publiques (OnlyOpen) : offres des recruteurs de test incluses (comptes de test, administration, propriétaire)
        public bool IncludeTestAccounts { get; set; }

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 12;
    }
}
