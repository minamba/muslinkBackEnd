using HelpMuslimWebApp.Api.ViewModels;

namespace HelpMuslimWebApp.Api.Utils
{
    // Fiche d'un candidat vue par un recruteur : seulement les coordonnées (et le CV) que le candidat a choisi de montrer,
    // et jamais ses vœux (« Ce que je cherche », salaire souhaité compris).
    // Le candidat lui-même et l'administration voient tout.
    public static class CandidateContact
    {
        public static CandidateProfileViewModel ApplyVisibility(CandidateProfileViewModel profile)
        {
            if (!profile.IsEmailVisible) profile.Email = null;
            if (!profile.IsPhoneVisible) profile.Phone = null;
            // CV masqué : ni visible ni téléchargeable depuis la fiche
            if (profile.IsCvHidden)
            {
                profile.HasCv = false;
                profile.CvFileName = null;
            }

            profile.WishProfessionIds = [];
            profile.WishContractTypes = [];
            profile.WishCities = [];
            profile.WishAnywhere = false;
            profile.WishRemote = false;
            profile.WishSalaryMin = null;

            // Abonnement : affaire du candidat (la mise en avant se voit au badge des listes)
            profile.IsPremium = false;
            profile.PremiumGrantedUntil = null;
            profile.PremiumPaidUntil = null;
            profile.HasPremiumSubscription = false;
            profile.PremiumCancelAtPeriodEnd = false;
            return profile;
        }

        // Listes (candidatures reçues, favoris, suggestions) : l'email n'y sert pas au recruteur
        public static CandidateSummaryViewModel? Strip(CandidateSummaryViewModel? candidate)
        {
            if (candidate != null) candidate.Email = null;
            return candidate;
        }
    }
}
