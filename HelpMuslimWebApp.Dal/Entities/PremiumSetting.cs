namespace HelpMuslimWebApp.Dal.Entities
{
    // Réglage unique (Id = 1) du premium, piloté depuis l'administration : un interrupteur par public.
    // Coupé : ni page Tarifs, ni avantages ou limites premium pour ce public.
    public partial class PremiumSetting
    {
        public int Id { get; set; }

        // Premium candidats
        public bool CandidatesEnabled { get; set; }
        // Prix mensuel TTC de l'abonnement candidat, en centimes (699 = 6,99 €)
        public int CandidateMonthlyPriceCents { get; set; }

        // Recruteur Pro (prix HT, en centimes)
        public bool RecruitersEnabled { get; set; }
        public int RecruiterMonthlyPriceCents { get; set; }
        public int RecruiterYearlyPriceCents { get; set; }
        // Offre sponsorisée à l'unité (30 jours)
        public int SponsoredOfferPriceCents { get; set; }

        // Offre de lancement : bandeaux « tout est offert » et places d'entreprise fondatrice (Recruteur Pro offert à vie)
        public bool LaunchOfferEnabled { get; set; }
        public int FounderSlots { get; set; }
        // Place attribuée automatiquement dès que les conditions sont remplies (sinon par l'administration)
        public bool FounderAutoGrant { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
