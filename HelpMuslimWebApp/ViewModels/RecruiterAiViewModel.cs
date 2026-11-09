namespace HelpMuslimWebApp.Api.ViewModels
{
    // État de l'assistant IA du recruteur connecté (Recruteur Pro)
    public class RecruiterAiViewModel
    {
        // Offre Recruteur Pro activée (coupée : assistant indisponible pour tous, et caché)
        public bool Enabled { get; set; }
        // Recruteur gratuit : l'assistant est réservé à Recruteur Pro
        public bool ProRequired { get; set; }
        // Clé Anthropic renseignée
        public bool Configured { get; set; }
        public bool Demo { get; set; }
        // Compte de test en mode test : sans quota
        public bool Unlimited { get; set; }
        // Rédactions et relectures d'offres (quota commun), brouillons de messages : par mois calendaire, heure de Paris
        public int OfferMonthlyLimit { get; set; }
        public int OfferRemaining { get; set; }
        public int MessageMonthlyLimit { get; set; }
        public int MessageRemaining { get; set; }
    }

    // ---------- Réponses du modèle (JSON au schéma de RecruiterAiClient)

    public class OfferDraftResult
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        // CDI | CDD | Alternance | Stage | Benevolat, vide si le texte ne le dit pas
        public string ContractType { get; set; } = string.Empty;
        // Noms pris dans le référentiel de Muslink (vides si rien ne convient)
        public string Domain { get; set; } = string.Empty;
        public string Profession { get; set; } = string.Empty;
        public List<string> RequiredSkills { get; set; } = [];
        public List<string> BonusSkills { get; set; } = [];
        public string City { get; set; } = string.Empty;
        public bool IsRemote { get; set; }
        public string Salary { get; set; } = string.Empty;
        public string Duration { get; set; } = string.Empty;
        // AAAA-MM-JJ, vide si non précisée
        public string StartDate { get; set; } = string.Empty;
        // -1 : non précisée
        public int ExperienceYears { get; set; } = -1;
        // Informations absentes du texte, à compléter par le recruteur
        public List<string> Missing { get; set; } = [];
        // Mentions du texte écartées (critères discriminatoires…)
        public List<string> Warnings { get; set; } = [];
    }

    public class OfferReviewIssue
    {
        // legal (à corriger : obligation légale) | important | tip
        public string Severity { get; set; } = "tip";
        // title | description | skills | conditions | general
        public string Field { get; set; } = "general";
        public string Problem { get; set; } = string.Empty;
        public string Suggestion { get; set; } = string.Empty;
    }

    public class OfferReviewResult
    {
        public string Summary { get; set; } = string.Empty;
        public List<OfferReviewIssue> Issues { get; set; } = [];
        // Vides quand l'offre n'a pas besoin d'être reformulée
        public string ImprovedTitle { get; set; } = string.Empty;
        public string ImprovedDescription { get; set; } = string.Empty;
        // Compétences présentes dans le texte mais absentes des champs
        public List<string> SuggestedRequiredSkills { get; set; } = [];
        public List<string> SuggestedBonusSkills { get; set; } = [];
    }

    public class MessageDraftResult
    {
        public string Message { get; set; } = string.Empty;
    }

    // ---------- Réponses de l'Api

    // Offre pré-remplie : les champs du formulaire, prêts à être relus
    public class OfferDraftViewModel
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? ContractType { get; set; }
        public int? DomainId { get; set; }
        public int? ProfessionId { get; set; }
        public List<string> RequiredSkills { get; set; } = [];
        public List<string> BonusSkills { get; set; } = [];
        public string? City { get; set; }
        public bool IsRemote { get; set; }
        public string? Salary { get; set; }
        public string? Duration { get; set; }
        public DateOnly? StartDate { get; set; }
        public int? ExperienceYears { get; set; }
        public List<string> Missing { get; set; } = [];
        public List<string> Warnings { get; set; } = [];
        public RecruiterAiViewModel State { get; set; } = new();
    }

    public class OfferReviewViewModel
    {
        public OfferReviewResult Review { get; set; } = new();
        public RecruiterAiViewModel State { get; set; } = new();
    }

    public class MessageDraftViewModel
    {
        public string Message { get; set; } = string.Empty;
        public RecruiterAiViewModel State { get; set; } = new();
    }

    // ---------- Analyse d'une offre : chiffres de Muslink (calculés sans IA), puis diagnostic et conseils

    public class OfferInsightsStats
    {
        public int DaysOnline { get; set; }
        public int Views { get; set; }
        public int Applications { get; set; }
        // Candidatures pour 100 vues (null : pas encore de vue)
        public int? ApplicationRate { get; set; }
        public bool IsOpen { get; set; }
        public bool IsHidden { get; set; }
        public bool IsSponsored { get; set; }

        // Candidats visibles sur Muslink
        public int CandidatesOnMuslink { get; set; }
        // Correspondance avec l'offre : forte (60 % et plus) ou partielle (30 à 59 %)
        public int StrongMatches { get; set; }
        public int PartialMatches { get; set; }

        // Candidats qui cherchent ce métier (« Ce que je cherche »), et ceux qui reçoivent l'alerte de l'offre (vœux à 70 % et plus)
        public int LookingForThisJob { get; set; }
        public int WishAlerted { get; set; }
        // Parmi ceux qui cherchent ce métier : ce qui les écarte
        public int ContractMismatch { get; set; }
        public int CityMismatch { get; set; }
        public int SalaryBelow { get; set; }
        public int WantRemote { get; set; }
        // Salaire annuel minimum souhaité (médiane, en euros) par ceux qui l'ont renseigné
        public int? MedianWishedSalary { get; set; }
        public int WishedSalaryCount { get; set; }
        // Salaire de l'offre lu dans le texte (annuel, en euros), null : absent ou illisible
        public int? OfferSalaryMin { get; set; }
        public int? OfferSalaryMax { get; set; }

        // Compétences requises : combien de candidats proches du poste (même domaine ou correspondance) les ont
        public int RelatedCandidates { get; set; }
        public List<SkillCoverage> Skills { get; set; } = [];

        // Offres ouvertes comparables (même métier, sinon même domaine)
        public int SimilarOffers { get; set; }
        public double? SimilarAverageApplications { get; set; }
        public double? SimilarAverageViews { get; set; }
        public int? SimilarWithSalaryPercent { get; set; }
        public int? SimilarRemotePercent { get; set; }
        public List<string> SimilarTopSkills { get; set; } = [];
    }

    public class SkillCoverage
    {
        public string Skill { get; set; } = string.Empty;
        public int Candidates { get; set; }
    }

    public class OfferInsightsRecommendation
    {
        public string Title { get; set; } = string.Empty;
        public string Detail { get; set; } = string.Empty;
        // Le chiffre sur lequel repose le conseil
        public string Evidence { get; set; } = string.Empty;
        // skills | salary | location | contract | title | description | visibility | other
        public string Kind { get; set; } = "other";
    }

    public class OfferInsightsResult
    {
        // trop_tot | bon | a_ameliorer | faible
        public string Verdict { get; set; } = "a_ameliorer";
        public string Summary { get; set; } = string.Empty;
        public List<OfferInsightsRecommendation> Recommendations { get; set; } = [];
    }

    public class OfferInsightsViewModel
    {
        public OfferInsightsStats Stats { get; set; } = new();
        public OfferInsightsResult Analysis { get; set; } = new();
        public RecruiterAiViewModel State { get; set; } = new();
    }

    // ---------- Questions d'entretien (à partir de l'offre seule)

    public class InterviewQuestion
    {
        public string Question { get; set; } = string.Empty;
        // Ce que la question permet de vérifier
        public string Purpose { get; set; } = string.Empty;
        // Ce qui montre une bonne réponse
        public string LookFor { get; set; } = string.Empty;
    }

    public class InterviewSection
    {
        public string Title { get; set; } = string.Empty;
        public List<InterviewQuestion> Questions { get; set; } = [];
    }

    public class InterviewQuestionsResult
    {
        public string Intro { get; set; } = string.Empty;
        public List<InterviewSection> Sections { get; set; } = [];
    }

    public class InterviewQuestionsViewModel
    {
        public InterviewQuestionsResult Interview { get; set; } = new();
        public RecruiterAiViewModel State { get; set; } = new();
    }
}
