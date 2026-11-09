using System.Security.Claims;
using AutoMapper;
using HelpMuslimWebApp.Api.Services;
using HelpMuslimWebApp.Api.Utils;
using HelpMuslimWebApp.Api.ViewModels;
using HelpMuslimWebApp.Domain.Models;
using HelpMuslimWebApp.Domain.Services;

namespace HelpMuslimWebApp.Api.Builders.impl
{
    public class FavoriteViewModelBuilder : IFavoriteViewModelBuilder
    {
        private readonly IMapper _mapper;
        private readonly IFavoriteService _favoriteService;
        private readonly ICandidateProfileService _candidateService;
        private readonly IProfileResolver _profileResolver;
        private readonly ICandidateProfileViewModelBuilder _candidateBuilder;

        public FavoriteViewModelBuilder(
            IMapper mapper,
            IFavoriteService favoriteService,
            ICandidateProfileService candidateService,
            IProfileResolver profileResolver,
            ICandidateProfileViewModelBuilder candidateBuilder)
        {
            _candidateBuilder = candidateBuilder;
            _mapper = mapper;
            _favoriteService = favoriteService;
            _candidateService = candidateService;
            _profileResolver = profileResolver;
        }

        public async Task<IEnumerable<FavoriteViewModel>> GetMyFavoritesAsync(ClaimsPrincipal user)
        {
            var recruiter = await _profileResolver.GetOrCreateRecruiterAsync(user);
            var favorites = _mapper.Map<List<FavoriteViewModel>>(await _favoriteService.GetFavoritesAsync(recruiter.Id));
            favorites.ForEach(f => CandidateContact.Strip(f.Candidate));
            return favorites;
        }

        public async Task<bool> AddFavoriteAsync(ClaimsPrincipal user, int candidateProfileId)
        {
            var recruiter = await _profileResolver.GetOrCreateRecruiterAsync(user);
            if (await _candidateService.GetCandidateProfileByIdAsync(candidateProfileId) is not { } candidate
                || (candidate.IsTestAccount && !user.SeesTestCandidates())) return false;
            // Recruteur gratuit (Recruteur Pro activé) : pas de favori sur un profil verrouillé
            await _candidateBuilder.EnsureRecruiterCanViewAsync(user, candidateProfileId);

            // Idempotent : un "like" déjà présent n'est pas une erreur
            if (!await _favoriteService.FavoriteExistsAsync(recruiter.Id, candidateProfileId))
                await _favoriteService.AddFavoriteAsync(new Favorite { RecruiterProfileId = recruiter.Id, CandidateProfileId = candidateProfileId });

            return true;
        }

        public async Task<bool> RemoveFavoriteAsync(ClaimsPrincipal user, int candidateProfileId)
        {
            var recruiter = await _profileResolver.GetOrCreateRecruiterAsync(user);
            return await _favoriteService.DeleteFavoriteAsync(recruiter.Id, candidateProfileId);
        }
    }
}
