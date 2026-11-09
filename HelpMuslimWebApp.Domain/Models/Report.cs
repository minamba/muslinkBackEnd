namespace HelpMuslimWebApp.Domain.Models
{
    // Signalement d'une offre, d'un candidat, d'une entreprise ou d'un recruteur, examiné par l'équipe (Administration › Signalements).
    // Le nom de l'élément signalé et celui de l'auteur sont gardés tels qu'au signalement : il reste lisible si l'élément disparaît.
    public class Report
    {
        public int Id { get; set; }
        public string TargetType { get; set; } = ReportTargets.Offer;
        public int TargetId { get; set; }
        public string TargetLabel { get; set; } = string.Empty;

        public string AuthorUserId { get; set; } = string.Empty;
        // « Candidat » ou « Recruteur »
        public string AuthorKind { get; set; } = string.Empty;
        public int? AuthorCandidateProfileId { get; set; }
        public int? AuthorRecruiterProfileId { get; set; }
        public string AuthorName { get; set; } = string.Empty;
        public string? AuthorEmail { get; set; }

        public string Reason { get; set; } = string.Empty;
        public string Justification { get; set; } = string.Empty;
        public string Status { get; set; } = ReportStatuses.New;
        // Justification de l'administrateur qui a annulé le signalement (facultative)
        public string? AdminNote { get; set; }
        public string? HandledBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? StatusChangedAt { get; set; }
    }

    public static class ReportTargets
    {
        public const string Offer = "offer";
        public const string Candidate = "candidate";
        public const string Company = "company";
        public const string Recruiter = "recruiter";

        public static readonly IReadOnlyList<string> All = [Offer, Candidate, Company, Recruiter];

        public static string Label(string type) => type switch
        {
            Offer => "Offre",
            Candidate => "Candidat",
            Company => "Entreprise",
            Recruiter => "Recruteur",
            _ => type,
        };
    }

    public static class ReportStatuses
    {
        public const string New = "new";
        public const string InReview = "in_review";
        public const string Cancelled = "cancelled";
        public const string Resolved = "resolved";

        public static readonly IReadOnlyList<string> All = [New, InReview, Cancelled, Resolved];

        public static string Label(string status) => status switch
        {
            New => "Nouveau",
            InReview => "En cours d'analyse",
            Cancelled => "Annulé",
            Resolved => "Traité",
            _ => status,
        };
    }

    // Motifs proposés (la justification, obligatoire, donne le détail)
    public static class ReportReasons
    {
        public static readonly IReadOnlyList<string> All =
        [
            "Arnaque ou fraude",
            "Annonce ou profil trompeur",
            "Discrimination",
            "Harcèlement ou propos inappropriés",
            "Faux profil ou usurpation d'identité",
            "Spam ou publicité",
            "Autre",
        ];
    }
}
