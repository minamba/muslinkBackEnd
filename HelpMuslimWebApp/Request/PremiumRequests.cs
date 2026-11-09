using System.ComponentModel.DataAnnotations;

namespace HelpMuslimWebApp.Api.Request
{
    // Administration : interrupteurs et prix (centimes : TTC pour les candidats, HT pour les recruteurs)
    public class PremiumSettingsRequest
    {
        public bool CandidatesEnabled { get; set; }

        [Range(50, 1_000_000, ErrorMessage = "Prix candidat invalide.")]
        public int CandidateMonthlyPriceCents { get; set; }

        public bool RecruitersEnabled { get; set; }

        [Range(50, 1_000_000, ErrorMessage = "Prix mensuel Recruteur Pro invalide.")]
        public int RecruiterMonthlyPriceCents { get; set; }

        [Range(50, 1_000_000, ErrorMessage = "Prix annuel Recruteur Pro invalide.")]
        public int RecruiterYearlyPriceCents { get; set; }

        [Range(50, 1_000_000, ErrorMessage = "Prix de l'offre sponsorisée invalide.")]
        public int SponsoredOfferPriceCents { get; set; }
    }

    // Administration : offrir le premium (candidat) ou le Pro (recruteur) pour 1, 3, 6 ou 12 mois, ou à vie
    public class PremiumGrantRequest
    {
        public int? Months { get; set; }
        public bool Lifetime { get; set; }
    }

    // Administration : offrir le Pro à tous les recruteurs actuels
    public class RecruiterBulkGrantRequest : PremiumGrantRequest
    {
        // false : seulement les recruteurs qui ont publié au moins une offre
        public bool IncludeWithoutOffers { get; set; }
    }

    // Recruteur : abonnement mensuel ou annuel
    public class RecruiterCheckoutRequest
    {
        [RegularExpression("^(month|year)$", ErrorMessage = "Périodicité invalide.")]
        public string Interval { get; set; } = "month";
    }
}
