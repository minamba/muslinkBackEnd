namespace HelpMuslimWebApp.Dal.Entities
{
    // Analyse du coach IA d'un candidat (supprimée avec son profil)
    public partial class CvCoachAnalysis
    {
        public int Id { get; set; }
        public int CandidateProfileId { get; set; }
        // UTC
        public DateTime CreatedAt { get; set; }
        // cv | profile
        public string Source { get; set; } = string.Empty;
        public string? CvFileName { get; set; }
        // Résultat JSON du modèle
        public string Result { get; set; } = string.Empty;
        public string? Model { get; set; }
        public int InputTokens { get; set; }
        public int OutputTokens { get; set; }
        // Lancée par le super admin (visible de lui seul)
        public bool ByAdmin { get; set; }
        // Supprimée par le candidat : résultat effacé, gardée pour le quota et les frais
        public DateTime? DeletedAt { get; set; }

        public virtual CandidateProfile? CandidateProfile { get; set; }
    }
}
