namespace HelpMuslimWebApp.Api.ViewModels
{
    // Coach IA du candidat : ce qui est possible (Premium, configuration, quota du mois) et la dernière analyse
    public class CvCoachViewModel
    {
        // Offre Premium candidats coupée dans l'administration : coach indisponible et caché (aucune analyse, aucun coût)
        public bool Enabled { get; set; }
        // Candidat non abonné : le coach est présenté, l'analyse est réservée aux abonnés
        public bool PremiumRequired { get; set; }
        // Clé de l'IA renseignée côté serveur
        public bool Configured { get; set; }
        // Démo : l'analyse déjà faite se lit, une nouvelle ne se lance pas
        public bool Demo { get; set; }

        public bool HasCv { get; set; }
        public string? CvFileName { get; set; }
        // CV lisible par le coach (PDF ou Word .docx) ; sinon l'analyse porte sur le profil
        public bool CvReadable { get; set; }
        // Profil assez rempli pour une analyse sans CV (expériences, formations, compétences ou présentation)
        public bool ProfileFilled { get; set; }
        // « Ce que je cherche » renseigné
        public bool HasWishes { get; set; }

        // Quota par mois calendaire (repart à zéro le 1er, heure de Paris)
        public int MonthlyLimit { get; set; }
        public int RemainingThisMonth { get; set; }
        // Sans quota : super admin, ou compte de test en mode test
        public bool Unlimited { get; set; }
        // Vue du super admin depuis l'administration (analyses visibles de lui seul)
        public bool ForAdmin { get; set; }
        // Compétences du profil : le front propose « Ajouter à mes compétences » pour les autres
        public List<string> ProfileSkills { get; set; } = [];

        public CvCoachAnalysisViewModel? Analysis { get; set; }
    }

    public class CvCoachAnalysisViewModel
    {
        public DateTime CreatedAt { get; set; }
        // cv | profile
        public string Source { get; set; } = string.Empty;
        public string? CvFileName { get; set; }
        public CvCoachResult Result { get; set; } = new();
    }

    // Réponse du modèle (sortie structurée) : chaque conseil cite l'endroit du CV ou du profil dont il vient
    public class CvCoachResult
    {
        public string Summary { get; set; } = string.Empty;
        public List<CvCoachStrength> Strengths { get; set; } = [];
        public List<CvCoachSkill> HighlightSkills { get; set; } = [];
        public List<CvCoachExperienceTip> ExperienceTips { get; set; } = [];
        public List<CvCoachQuestion> Questions { get; set; } = [];
        public List<CvCoachMissing> Missing { get; set; } = [];
        // Adéquation avec « Ce que je cherche » (vide sans vœux)
        public string TargetFit { get; set; } = string.Empty;
    }

    public class CvCoachStrength
    {
        public string Title { get; set; } = string.Empty;
        public string Detail { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
    }

    public class CvCoachSkill
    {
        public string Skill { get; set; } = string.Empty;
        public string Why { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
    }

    public class CvCoachExperienceTip
    {
        public string Experience { get; set; } = string.Empty;
        public string Advice { get; set; } = string.Empty;
        public string Example { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
    }

    public class CvCoachQuestion
    {
        public string Question { get; set; } = string.Empty;
        public string Why { get; set; } = string.Empty;
    }

    public class CvCoachMissing
    {
        public string Item { get; set; } = string.Empty;
        public string Advice { get; set; } = string.Empty;
    }
}
