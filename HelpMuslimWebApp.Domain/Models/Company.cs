namespace HelpMuslimWebApp.Domain.Models
{
    // Entreprise (vue publique d'un profil recruteur) : annuaire, page entreprise, entreprises suivies
    public class Company
    {
        public int Id { get; set; }
        public string? UserId { get; set; }     // compte du recruteur (non exposé) : il voit sa page même désactivée
        public string? CompanyName { get; set; }
        public string? LogoPath { get; set; }
        public string? CompanySector { get; set; }
        public string? CompanySize { get; set; }
        public string? City { get; set; }
        public string? CompanyWebsite { get; set; }
        public string? CompanyDescription { get; set; }

        // Offres ouvertes et visibles publiquement
        public int OpenOffersCount { get; set; }

        // Désactivée par l'administration : absente de l'annuaire, offres cachées
        public bool IsDisabled { get; set; }

        // Entreprise fondatrice (Pro offert à vie) : badge sur sa page
        public bool IsFounder { get; set; }

        // Recruteur de test : entreprise visible seulement des comptes de test et de l'administration
        public bool IsTestAccount { get; set; }

        // Administration : recruteur rattaché et nombre total d'offres
        public string? RecruiterName { get; set; }
        public string? RecruiterEmail { get; set; }
        public int OffersCount { get; set; }
        public DateTime? DisabledAt { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    // Activité présente dans l'annuaire et nombre d'entreprises qui l'exercent (filtre de l'annuaire)
    public class CompanySectorCount
    {
        public string Sector { get; set; } = "";
        public int Count { get; set; }
    }

    // Candidat qui suit une entreprise : destinataire des alertes de nouvelle offre
    public class CompanyFollower
    {
        public int CandidateProfileId { get; set; }
        public string? UserId { get; set; }
        public string? Email { get; set; }
        public string? FirstName { get; set; }
        public bool IsTestAccount { get; set; }

        // Premium : seuls les abonnés reçoivent l'email (la notification sur Muslink est pour tous)
        public DateTime? PremiumGrantedUntil { get; set; }
        public DateTime? PremiumPaidUntil { get; set; }
        public DateTime? PremiumUntil => Premium.Until(PremiumGrantedUntil, PremiumPaidUntil);
    }
}
