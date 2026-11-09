namespace HelpMuslimWebApp.Domain.Models
{
    // Offre et sa correspondance avec le profil d'un candidat
    public class OfferMatch
    {
        public JobOffer Offer { get; set; } = new();

        // 0 à 100
        public int Score { get; set; }
        public List<string> Reasons { get; set; } = [];

        // Compétences de l'offre (requises puis bonus) que le candidat possède
        public List<string> MatchedSkills { get; set; } = [];
    }

    // Une compétence de l'offre face au profil d'un candidat : CandidateSkill = sa compétence qui la couvre (« React.js » pour
    // « React »), null si elle lui manque
    public record SkillCheck(string Skill, bool Required, string? CandidateSkill);
}
