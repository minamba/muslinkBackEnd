namespace HelpMuslimWebApp.Domain.Models
{
    public static class ContractTypes
    {
        public const string Cdd = "CDD";
        public const string Cdi = "CDI";
        public const string Stage = "Stage";
        public const string Alternance = "Alternance";
        public const string Benevolat = "Benevolat";

        public static readonly IReadOnlyList<string> All = [Cdi, Cdd, Alternance, Stage, Benevolat];

        // Libellé affiché (emails)
        public static string? Label(string? contractType) => contractType == Benevolat ? "Bénévolat" : contractType;
    }

    public static class CandidateSituations
    {
        public const string Etudiant = "Etudiant";
        public const string EnPoste = "EnPoste";
        public const string EnRecherche = "EnRecherche";

        public static readonly IReadOnlyList<string> All = [Etudiant, EnPoste, EnRecherche];
    }

    public static class OfferStatuses
    {
        public const string Nouveau = "Nouveau";
        public const string Ouverte = "Ouverte";
        public const string Fermee = "Fermee";
        public const string EnDiscussion = "EnDiscussion";
        public const string Pourvue = "Pourvue";

        // Fermée par le recruteur : Pourvue s'il a trouvé son candidat, sinon Fermée.
        // Ouverte : En discussion dès que le recruteur écrit à un candidat qui y a postulé,
        // sinon Nouveau (moins d'un mois) ou Ouverte.
        public static string Compute(bool? isOpen, DateTime? createdAt, DateTime? inDiscussionAt = null, bool? isFilled = null)
        {
            if (isOpen != true) return isFilled == true ? Pourvue : Fermee;
            if (inDiscussionAt != null) return EnDiscussion;
            return createdAt > DateTime.UtcNow.AddMonths(-1) ? Nouveau : Ouverte;
        }
    }
}
