namespace HelpMuslimWebApp.Dal.Entities
{
    public partial class RecruiterProfile
    {
        public int Id { get; set; }
        public string? UserId { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }

        // Le recruteur affiche son numéro sur ses offres (masqué par défaut)
        public bool? IsPhoneVisible { get; set; }
        public string? JobTitle { get; set; }
        public string? CompanyName { get; set; }
        public string? CompanySector { get; set; }
        public string? CompanySize { get; set; }
        public string? CompanyWebsite { get; set; }
        public string? CompanyDescription { get; set; }
        public string? City { get; set; }
        public string? LogoPath { get; set; }

        // Page entreprise enrichie (« Mon entreprise › Ma page entreprise ») : photo de couverture, avantages, ambiance,
        // profils recherchés, étapes du recrutement, année de création, réseaux sociaux ; photos et vidéos dans CompanyMedia
        public string? CoverPath { get; set; }
        public string[]? Benefits { get; set; }
        public string? Culture { get; set; }
        public string? LookingFor { get; set; }
        public string[]? RecruitmentSteps { get; set; }
        public int? FoundedYear { get; set; }
        public string? LinkedinUrl { get; set; }
        public string? InstagramUrl { get; set; }
        public string? FacebookUrl { get; set; }
        public string? YoutubeUrl { get; set; }

        // Désactivée par l'administration : l'entreprise et toutes ses offres disparaissent du public
        public bool? IsDisabled { get; set; }
        public DateTime? DisabledAt { get; set; }

        // Compte de test (IdentityServer) : ses offres ne reçoivent que des candidatures de comptes de test
        public bool IsTestAccount { get; set; }

        // Recruteur Pro : offert depuis l'administration jusqu'à… (à vie = 31/12/9999), payé (abonnement Stripe) jusqu'à…
        public DateTime? ProGrantedUntil { get; set; }
        public DateTime? ProPaidUntil { get; set; }
        // Entreprise fondatrice : Pro offert à vie (badge sur sa page et ses offres)
        public bool? IsFounder { get; set; }
        public string? StripeCustomerId { get; set; }
        public string? StripeSubscriptionId { get; set; }
        public bool? ProCancelAtPeriodEnd { get; set; }
        // Offre sponsorisée incluse dans le Pro : une par mois
        public DateTime? LastFreeSponsorAt { get; set; }
        // Alerte quotidienne « nouveaux candidats à 70 % » : coupée par le recruteur (compte ou lien de l'email)
        public bool? MatchAlertsDisabled { get; set; }
        // Jeton du lien de désinscription de l'email (créé au premier envoi)
        public string? MatchAlertsToken { get; set; }
        // Email « un candidat vous a écrit » : coupé par le recruteur (compte ou lien de l'email, même jeton)
        public bool? MessageAlertsDisabled { get; set; }
        // Email « nouvelle candidature » : coupé par le recruteur (compte ou lien de l'email, même jeton)
        public bool? ApplicationAlertsDisabled { get; set; }

        // Identité légale : SIRET vérifié auprès de l'annuaire des entreprises (un compte par SIREN),
        // ou numéro RNA d'une association, validé par l'administration
        public string? Siret { get; set; }
        public string? Siren { get; set; }
        public string? Rna { get; set; }
        public string? LegalName { get; set; }
        // Noms officiels normalisés (raison sociale, sigle, enseignes…) séparés par « | » : le nom affiché doit en faire partie
        public string? LegalAliases { get; set; }
        public DateTime? IdentityVerifiedAt { get; set; }
        // Dernier état connu de l'adresse email du compte (confirmée par le lien de l'email ou Google)
        public bool? IsEmailConfirmed { get; set; }
        // Empreinte de l'adresse IP à la création du profil : signale à l'administration les comptes créés en série
        public string? SignupIpHash { get; set; }
        // Entreprise fondatrice depuis le…
        public DateTime? FounderSince { get; set; }
        // Statut retiré par l'administration : plus jamais attribué automatiquement
        public DateTime? FounderRevokedAt { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public virtual ICollection<JobOffer> Offers { get; set; } = new List<JobOffer>();
        public virtual ICollection<CompanyMedia> Media { get; set; } = new List<CompanyMedia>();
    }
}
