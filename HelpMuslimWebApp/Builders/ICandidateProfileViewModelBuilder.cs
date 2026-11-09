using System.Security.Claims;
using HelpMuslimWebApp.Api.Request;
using HelpMuslimWebApp.Api.ViewModels;

namespace HelpMuslimWebApp.Api.Builders
{
    public interface ICandidateProfileViewModelBuilder
    {
        // Recruteur Pro activé, recruteur gratuit : refuse (409) un candidat hors de ses profils visibles et sans lien avec lui
        Task EnsureRecruiterCanViewAsync(ClaimsPrincipal viewer, int candidateId);
        Task<IEnumerable<CandidateSummaryViewModel>> GetCandidateProfilesAsync(string? search);

        // Recruteur : candidats triés du plus proche au plus éloigné de ses offres ouvertes (ou de l'une d'elles), filtrés
        Task<IEnumerable<CandidateMatchViewModel>> GetMatchingCandidatesAsync(ClaimsPrincipal user, CandidateMatchQuery query);
        // Recruteur : nombre de candidats en lien avec chacune de ses offres ouvertes (filtre par offre)
        Task<IEnumerable<OfferMatchCountViewModel>> GetMatchingOfferCountsAsync(ClaimsPrincipal user, CandidateMatchQuery query);
        // Recruteur : villes des candidats (filtre « Ville »)
        Task<IEnumerable<CityCountViewModel>> GetMatchingCitiesAsync(ClaimsPrincipal user);
        Task<CandidateProfileViewModel?> GetCandidateProfileByIdAsync(int id, ClaimsPrincipal viewer);
        Task<CandidateProfileViewModel> GetMyProfileAsync(ClaimsPrincipal user);
        Task<CandidateProfileViewModel?> UpdateMyProfileAsync(ClaimsPrincipal user, CandidateProfileRequest model);
        Task<CandidateProfileViewModel?> UpdateCandidateProfileAsync(int id, AdminCandidateProfileRequest model);   // compte de connexion mis à jour aussi
        Task<CandidateProfileViewModel?> AddCandidateProfileAsync(ProfileCreateRequest model);  // null = profil déjà existant
        Task<bool> DeleteCandidateProfileAsync(int id);

        Task<CandidateProfileViewModel> UpdateMyPhotoAsync(ClaimsPrincipal user, IFormFile file);
        Task<CandidateProfileViewModel> DeleteMyPhotoAsync(ClaimsPrincipal user);
        Task<CandidateProfileViewModel> UpdateMyCvAsync(ClaimsPrincipal user, IFormFile file);
        Task<CandidateProfileViewModel> DeleteMyCvAsync(ClaimsPrincipal user);
        // CV montré ou masqué aux recruteurs sur la fiche
        Task<CandidateProfileViewModel> SetMyCvHiddenAsync(ClaimsPrincipal user, bool hidden);
        Task<(Stream Stream, string FileName, string ContentType)?> GetCvAsync(int candidateId, ClaimsPrincipal viewer);

        // Profil invisible des recruteurs (suggestions, recherche)
        Task<CandidateProfileViewModel> SetMyVisibilityAsync(ClaimsPrincipal user, bool isHidden);

        // Fiche partagée par QR code
        Task<CandidateShareViewModel> GetMyShareAsync(ClaimsPrincipal user);
        Task<CandidateShareViewModel> EnableMyShareAsync(ClaimsPrincipal user, CandidateShareRequest model);
        Task<CandidateShareViewModel> DisableMyShareAsync(ClaimsPrincipal user);
        Task<SharedCandidateProfileViewModel?> GetSharedProfileAsync(string token);
    }
}
