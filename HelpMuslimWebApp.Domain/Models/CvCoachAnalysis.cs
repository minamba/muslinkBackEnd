namespace HelpMuslimWebApp.Domain.Models
{
    // Ce que le coach a lu pour son analyse
    public static class CvCoachSources
    {
        // CV déposé (PDF ou Word .docx), complété par le profil
        public const string Cv = "cv";
        // Pas de CV lisible : le profil rempli sur Muslink
        public const string Profile = "profile";
    }

    // Analyse du coach IA (candidat Premium) : gardée pour être relue sans relancer l'IA
    public class CvCoachAnalysis
    {
        public int Id { get; set; }
        public int CandidateProfileId { get; set; }
        // UTC
        public DateTime CreatedAt { get; set; }
        public string Source { get; set; } = CvCoachSources.Profile;
        public string? CvFileName { get; set; }
        // Résultat (JSON produit par le modèle, au schéma de CvCoachResult côté Api)
        public string Result { get; set; } = string.Empty;
        public string? Model { get; set; }
        public int InputTokens { get; set; }
        public int OutputTokens { get; set; }
        // Lancée par le super admin depuis l'administration : visible de lui seul, hors quota du candidat
        public bool ByAdmin { get; set; }
        // Supprimée par le candidat (UTC) : résultat effacé, la ligne reste pour le quota du mois et les frais
        public DateTime? DeletedAt { get; set; }
    }
}
