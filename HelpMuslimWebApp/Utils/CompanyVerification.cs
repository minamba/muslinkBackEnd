using HelpMuslimWebApp.Domain.Models;

namespace HelpMuslimWebApp.Api.Utils
{
    // Entreprise vérifiée (SIRET contrôlé auprès de l'annuaire officiel, ou RNA validé) : obligatoire pour publier une offre
    // et pour accéder aux candidats — derrière chaque profil, il y a un vrai candidat
    public static class CompanyVerification
    {
        public const string PublishMessage = "Pour publier une offre, faites vérifier votre entreprise : renseignez son nom officiel et son numéro SIRET dans « Mon entreprise ».";
        public const string CandidatesMessage = "Pour accéder aux candidats, faites vérifier votre entreprise : renseignez son nom officiel et son numéro SIRET dans « Mon entreprise ».";
        public const string PendingMessage = "Votre numéro RNA est en cours de validation par l'équipe Muslink : vous pourrez publier des offres et accéder aux candidats dès qu'il sera validé.";

        public static bool IsVerified(RecruiterProfile recruiter) => recruiter.IdentityVerifiedAt != null;

        public static void Ensure(RecruiterProfile recruiter, string message)
        {
            if (IsVerified(recruiter)) return;
            throw new InvalidOperationException(recruiter.Rna != null && recruiter.Siret == null ? PendingMessage : message);
        }
    }
}
