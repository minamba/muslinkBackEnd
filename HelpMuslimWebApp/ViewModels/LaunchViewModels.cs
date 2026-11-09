namespace HelpMuslimWebApp.Api.ViewModels
{
    // Offre de lancement (bandeaux, page /offre-de-lancement) : places d'entreprise fondatrice restantes
    public class LaunchViewModel
    {
        public bool Enabled { get; set; }
        public int Slots { get; set; }
        public int Remaining { get; set; }
        // Recruteur connecté : où il en est des conditions
        public FounderStatusViewModel? Recruiter { get; set; }
    }

    public class FounderStatusViewModel
    {
        public bool IsFounder { get; set; }
        public DateTime? FounderSince { get; set; }
        // Statut retiré par l'administration : plus de place proposée
        public bool IsRevoked { get; set; }
        // Place attribuée automatiquement dès que les conditions sont remplies (sinon après examen par l'équipe)
        public bool AutoGrant { get; set; }
        public bool HasIdentity { get; set; }
        public bool IdentityVerified { get; set; }
        // RNA (ou SIRET pas encore vérifiable) : en attente de validation
        public bool IdentityPending { get; set; }
        public bool EmailConfirmed { get; set; }
        public bool HasPublishedOffer { get; set; }
    }

    // Administration : réglages de l'offre de lancement et revue des entreprises
    public class FounderAdminViewModel
    {
        public bool Enabled { get; set; }
        public int Slots { get; set; }
        public bool AutoGrant { get; set; }
        public int Founders { get; set; }
        public int Remaining { get; set; }
        public List<FounderReviewViewModel> Items { get; set; } = [];
    }

    public class FounderReviewViewModel
    {
        public int RecruiterProfileId { get; set; }
        public string? CompanyName { get; set; }
        public string? RecruiterName { get; set; }
        public string? Email { get; set; }
        // Domaine de l'email ; messagerie grand public (gmail.com, orange.fr…) plutôt qu'un domaine d'entreprise
        public string? EmailDomain { get; set; }
        public bool IsWebmail { get; set; }
        public bool IsEmailConfirmed { get; set; }
        public bool IsFounder { get; set; }
        public DateTime? FounderSince { get; set; }
        // Statut retiré par l'administration : plus attribué automatiquement
        public bool IsRevoked { get; set; }
        public string? Siret { get; set; }
        public string? Rna { get; set; }
        public string? LegalName { get; set; }
        public bool IdentityVerified { get; set; }
        public DateTime? FirstOfferAt { get; set; }
        public int OffersCount { get; set; }
        public bool IsDisabled { get; set; }
        // Toutes les conditions remplies (identité, email confirmé, offre publiée)
        public bool IsEligible { get; set; }
        // Autres comptes de la revue créés depuis la même connexion
        public List<string> SharedIpWith { get; set; } = [];
        public DateTime? CreatedAt { get; set; }
    }
}
