using System.Security.Claims;
using AutoMapper;
using HelpMuslimWebApp.Api.Request;
using HelpMuslimWebApp.Api.Services;
using HelpMuslimWebApp.Api.Utils;
using HelpMuslimWebApp.Api.ViewModels;
using HelpMuslimWebApp.Domain.Models;
using HelpMuslimWebApp.Domain.Security;
using HelpMuslimWebApp.Domain.Services;

namespace HelpMuslimWebApp.Api.Builders.impl
{
    public class CandidateProfileViewModelBuilder : ICandidateProfileViewModelBuilder
    {
        private readonly IMapper _mapper;
        private readonly IPremiumService _premiumService;
        private readonly TestModeGuard _testMode;
        private readonly ICandidateProfileService _candidateService;
        private readonly IProfessionService _professionService;
        private readonly IFavoriteService _favoriteService;
        private readonly IProfileResolver _profileResolver;
        private readonly IFileStorageService _fileStorage;
        private readonly IJobOfferService _jobOfferService;
        private readonly IJobApplicationService _applicationService;
        private readonly IConversationService _conversationService;
        private readonly IIdentityAccountClient _accountClient;
        private readonly ILogger<CandidateProfileViewModelBuilder> _logger;

        public CandidateProfileViewModelBuilder(
            IMapper mapper,
            IPremiumService premiumService,
            TestModeGuard testMode,
            ICandidateProfileService candidateService,
            IProfessionService professionService,
            IFavoriteService favoriteService,
            IProfileResolver profileResolver,
            IFileStorageService fileStorage,
            IJobOfferService jobOfferService,
            IJobApplicationService applicationService,
            IConversationService conversationService,
            IIdentityAccountClient accountClient,
            ILogger<CandidateProfileViewModelBuilder> logger)
        {
            _mapper = mapper;
            _premiumService = premiumService;
            _testMode = testMode;
            _candidateService = candidateService;
            _professionService = professionService;
            _favoriteService = favoriteService;
            _profileResolver = profileResolver;
            _fileStorage = fileStorage;
            _jobOfferService = jobOfferService;
            _applicationService = applicationService;
            _conversationService = conversationService;
            _accountClient = accountClient;
            _logger = logger;
        }

        public async Task<IEnumerable<CandidateSummaryViewModel>> GetCandidateProfilesAsync(string? search) =>
            _mapper.Map<IEnumerable<CandidateSummaryViewModel>>(await _candidateService.GetCandidateProfilesAsync(search));

        // Message renvoyé à un recruteur gratuit qui ouvre la fiche ou le CV d'un candidat verrouillé
        public const string LockedProfileMessage = "Ce profil est réservé à Recruteur Pro : passez à Recruteur Pro pour voir tous les candidats qui correspondent à vos offres.";

        public async Task<IEnumerable<CandidateMatchViewModel>> GetMatchingCandidatesAsync(ClaimsPrincipal user, CandidateMatchQuery query)
        {
            var recruiter = await _profileResolver.GetOrCreateRecruiterAsync(user);
            var exempt = await _testMode.ExemptsAsync(user);
            if (!exempt) CompanyVerification.Ensure(recruiter, CompanyVerification.CandidatesMessage);
            var settings = await _premiumService.GetSettingsAsync();
            bool IsPremium(CandidateSummary c) => settings.CandidatesEnabled && Premium.IsActive(c.PremiumUntil);
            var ranked = await RankForRecruiterAsync(recruiter, settings, query);
            var favorites = (await _favoriteService.GetFavoritesAsync(recruiter.Id)).Select(f => f.CandidateProfileId).ToHashSet();

            // Recruteur Pro activé : un recruteur gratuit voit ses 3 profils les plus proches (avec ou sans recherche ni filtre : une
            // recherche par nom ou un filtre ne dévoile pas les autres), les suivants sont verrouillés (score et offre seulement,
            // sans identité) ; un profil sans aucun lien avec ses offres (0 %) n'est pas montré verrouillé
            var restricted = !exempt && settings.RecruitersEnabled && !Premium.IsActive(recruiter.ProUntil);
            var visibleIds = !restricted ? null : !query.IsFiltered
                ? ranked.Take(Premium.FreeVisibleMatches).Select(m => m.Candidate.Id).ToHashSet()
                : (await RankForRecruiterAsync(recruiter, settings, new CandidateMatchQuery())).Take(Premium.FreeVisibleMatches).Select(m => m.Candidate.Id).ToHashSet();
            bool Locked(CandidateMatch m) => visibleIds != null && !visibleIds.Contains(m.Candidate.Id);
            ranked = ranked.Where(m => !Locked(m) || m.Score > 0).ToList();

            return ranked.Take(query.Take is > 0 ? query.Take.Value : int.MaxValue).Select(m =>
            {
                if (Locked(m))
                    return new CandidateMatchViewModel { IsLocked = true, Score = m.Score, OfferId = m.OfferId, OfferTitle = m.OfferTitle };

                var candidate = CandidateContact.Strip(_mapper.Map<CandidateSummaryViewModel>(m.Candidate))!;
                candidate.IsPremium = IsPremium(m.Candidate);
                return new CandidateMatchViewModel
                {
                    Candidate = candidate,
                    Score = m.Score,
                    OfferId = m.OfferId,
                    OfferTitle = m.OfferTitle,
                    Reasons = m.Reasons,
                    IsFavorite = favorites.Contains(m.Candidate.Id),
                };
            });
        }

        // Candidats classés pour un recruteur (offres ouvertes, ou l'une d'elles), du plus proche au plus éloigné ;
        // premium candidats activé : bonus de classement pour les candidats premium (le score reste le vrai)
        private async Task<List<CandidateMatch>> RankForRecruiterAsync(RecruiterProfile recruiter, PremiumSettings settings, CandidateMatchQuery query)
        {
            IReadOnlyCollection<JobOffer> offers = await OpenOffersAsync(recruiter);
            if (query.OfferId is { } offerId) offers = offers.Where(o => o.Id == offerId).ToList();
            var candidates = Filter(await CandidatesForAsync(recruiter), query);

            bool IsPremium(CandidateSummary c) => settings.CandidatesEnabled && Premium.IsActive(c.PremiumUntil);
            return CandidateMatcher.Rank(candidates, offers)
                .OrderByDescending(m => Premium.RankingKey(m.Score, IsPremium(m.Candidate)))
                .ThenByDescending(m => m.Score)
                .ThenByDescending(m => m.Candidate.CreatedAt)
                .ToList();
        }

        // Candidats montrés à un recruteur : les comptes de test seulement à un recruteur de test
        private async Task<IEnumerable<CandidateSummary>> CandidatesForAsync(RecruiterProfile recruiter) =>
            (await _candidateService.GetCandidatesForMatchingAsync()).Where(c => !c.IsTestAccount || recruiter.IsTestAccount);

        private async Task<IReadOnlyCollection<JobOffer>> OpenOffersAsync(RecruiterProfile recruiter) =>
            (await _jobOfferService.GetJobOffersAsync(new OfferFilter { RecruiterProfileId = recruiter.Id, OnlyOpen = true, IncludeTestAccounts = true, PageSize = 50 })).Items;

        // Recherche libre, ville et disponibilité (l'offre choisie, elle, restreint les offres et non les candidats)
        private static IEnumerable<CandidateSummary> Filter(IEnumerable<CandidateSummary> candidates, CandidateMatchQuery query)
        {
            if (!string.IsNullOrWhiteSpace(query.Search))
            {
                var term = Fold(query.Search);
                candidates = candidates.Where(c => Fold(string.Join(' ',
                        [c.FirstName, c.LastName, c.Headline, c.DesiredJob, c.DesiredProfessionName, c.City, .. c.Skills])).Contains(term));
            }
            if (!string.IsNullOrWhiteSpace(query.City))
            {
                var city = Fold(query.City);
                candidates = candidates.Where(c => Fold(c.City) == city);
            }
            // Disponibilité : seuls les candidats qui l'ont renseignée, disponibles au plus tard dans N mois
            if (query.AvailableWithinMonths is { } months)
            {
                var limit = DateOnly.FromDateTime(DateTime.Today).AddMonths(Math.Clamp(months, 0, 12));
                candidates = candidates.Where(c => c.AvailableFrom != null && c.AvailableFrom <= limit);
            }
            return candidates;
        }

        // Pastilles « N pour <offre> » : candidats qui ont un lien avec chaque offre ouverte (recherche, ville et disponibilité
        // appliquées), c'est-à-dire ceux qu'affiche le filtre sur cette offre
        public async Task<IEnumerable<OfferMatchCountViewModel>> GetMatchingOfferCountsAsync(ClaimsPrincipal user, CandidateMatchQuery query)
        {
            var recruiter = await _profileResolver.GetOrCreateRecruiterAsync(user);
            if (!await _testMode.ExemptsAsync(user)) CompanyVerification.Ensure(recruiter, CompanyVerification.CandidatesMessage);
            var offers = await OpenOffersAsync(recruiter);
            var counts = offers.ToDictionary(o => o.Id, _ => 0);
            foreach (var candidate in Filter(await CandidatesForAsync(recruiter), query))
                foreach (var match in CandidateMatcher.RankOffers(candidate, offers).Where(m => m.Score > 0))
                    counts[match.Offer.Id]++;
            return offers
                .Select(o => new OfferMatchCountViewModel(o.Id, o.Title ?? string.Empty, counts[o.Id]))
                .OrderByDescending(o => o.Count).ThenBy(o => o.Title)
                .ToList();
        }

        // Villes des candidats visibles (filtre « Ville »), écrites comme la plupart l'écrivent, avec leur nombre
        public async Task<IEnumerable<CityCountViewModel>> GetMatchingCitiesAsync(ClaimsPrincipal user)
        {
            var recruiter = await _profileResolver.GetOrCreateRecruiterAsync(user);
            if (!await _testMode.ExemptsAsync(user)) CompanyVerification.Ensure(recruiter, CompanyVerification.CandidatesMessage);
            return (await CandidatesForAsync(recruiter))
                .Where(c => !string.IsNullOrWhiteSpace(c.City))
                .GroupBy(c => Fold(c.City))
                .Select(g => new CityCountViewModel(
                    g.GroupBy(c => c.City!.Trim()).OrderByDescending(s => s.Count()).First().Key,
                    g.Count()))
                .OrderBy(c => Fold(c.City))
                .ToList();
        }

        private static string Fold(string? value) => SearchText.RemoveAccents((value ?? string.Empty).Trim()).ToLowerInvariant();

        public async Task EnsureRecruiterCanViewAsync(ClaimsPrincipal viewer, int candidateId) =>
            await EnsureFreeRecruiterCanViewAsync(await _profileResolver.FindRecruiterAsync(viewer), candidateId, viewer);

        // Recruteur Pro activé, recruteur gratuit : fiche, CV et favori des seuls candidats qu'il peut voir — ses 3 profils
        // visibles et les candidats qui ont postulé chez lui ou avec qui il échange
        private async Task EnsureFreeRecruiterCanViewAsync(RecruiterProfile? recruiter, int candidateId, ClaimsPrincipal viewer)
        {
            if (recruiter == null || !viewer.IsRecruteur() || viewer.HasPermission(Permissions.Candidates.View)) return;
            if (await _testMode.ExemptsAsync(viewer)) return;
            // Fiche, CV et favori : entreprise vérifiée (SIRET ou RNA)
            CompanyVerification.Ensure(recruiter, CompanyVerification.CandidatesMessage);
            if (Premium.IsActive(recruiter.ProUntil)) return;
            var settings = await _premiumService.GetSettingsAsync();
            if (!settings.RecruitersEnabled) return;
            if (await _candidateService.HasRelationWithRecruiterAsync(candidateId, recruiter.Id)) return;

            var visible = (await RankForRecruiterAsync(recruiter, settings, new CandidateMatchQuery())).Take(Premium.FreeVisibleMatches).Select(m => m.Candidate.Id);
            if (!visible.Contains(candidateId)) throw new InvalidOperationException(LockedProfileMessage);
        }

        public async Task<CandidateProfileViewModel?> GetCandidateProfileByIdAsync(int id, ClaimsPrincipal viewer)
        {
            var profile = await _candidateService.GetCandidateProfileByIdAsync(id);
            if (profile == null) return null;

            // Visible par les recruteurs, le candidat lui-même et l'administration
            var isSelf = profile.UserId == viewer.GetUserId();
            if (!isSelf && !viewer.IsRecruteur() && !viewer.HasPermission(Permissions.Candidates.View))
                throw new UnauthorizedAccessException();

            var recruiter = await _profileResolver.FindRecruiterAsync(viewer);

            // Profil invisible : seuls les recruteurs déjà en relation (candidature, messagerie) y ont accès
            if (profile.IsHidden == true && !isSelf && !viewer.HasPermission(Permissions.Candidates.View)
                && (recruiter == null || !await _candidateService.HasRelationWithRecruiterAsync(profile.Id, recruiter.Id)))
                return null;
            // Compte de test : invisible des vrais recruteurs, même s'il a postulé chez eux
            if (profile.IsTestAccount && !isSelf && !viewer.SeesTestCandidates()) return null;

            if (!isSelf) await EnsureFreeRecruiterCanViewAsync(recruiter, profile.Id, viewer);

            var viewModel = _mapper.Map<CandidateProfileViewModel>(profile);
            if (recruiter != null)
                viewModel.IsFavorite = await _favoriteService.FavoriteExistsAsync(recruiter.Id, profile.Id);

            // Recruteur : email et téléphone seulement si le candidat a choisi de les montrer
            if (!isSelf && !viewer.HasPermission(Permissions.Candidates.View)) CandidateContact.ApplyVisibility(viewModel);

            return viewModel;
        }

        public async Task<CandidateProfileViewModel> SetMyVisibilityAsync(ClaimsPrincipal user, bool isHidden)
        {
            var profile = await _profileResolver.GetOrCreateCandidateAsync(user);
            await _candidateService.SetCandidateHiddenAsync(profile.Id, isHidden);
            profile.IsHidden = isHidden;
            return _mapper.Map<CandidateProfileViewModel>(profile);
        }

        public async Task<CandidateShareViewModel> GetMyShareAsync(ClaimsPrincipal user)
        {
            var profile = await _profileResolver.GetOrCreateCandidateAsync(user);
            return new CandidateShareViewModel { IsEnabled = profile.ShareToken != null, Token = profile.ShareToken, ShareContact = profile.ShareContact == true };
        }

        public async Task<CandidateShareViewModel> EnableMyShareAsync(ClaimsPrincipal user, CandidateShareRequest model)
        {
            var profile = await _profileResolver.GetOrCreateCandidateAsync(user);
            var token = await _candidateService.EnableShareAsync(profile, model.ShareContact, model.Renew);
            return new CandidateShareViewModel { IsEnabled = true, Token = token, ShareContact = model.ShareContact };
        }

        public async Task<CandidateShareViewModel> DisableMyShareAsync(ClaimsPrincipal user)
        {
            var profile = await _profileResolver.GetOrCreateCandidateAsync(user);
            await _candidateService.DisableShareAsync(profile.Id);
            return new CandidateShareViewModel { IsEnabled = false };
        }

        public async Task<SharedCandidateProfileViewModel?> GetSharedProfileAsync(string token)
        {
            var profile = await _candidateService.GetSharedCandidateProfileAsync(token);
            return profile == null ? null : _mapper.Map<SharedCandidateProfileViewModel>(profile);
        }

        public async Task<CandidateProfileViewModel> GetMyProfileAsync(ClaimsPrincipal user) =>
            _mapper.Map<CandidateProfileViewModel>(await _profileResolver.GetOrCreateCandidateAsync(user));

        public async Task<CandidateProfileViewModel?> UpdateMyProfileAsync(ClaimsPrincipal user, CandidateProfileRequest model)
        {
            var profile = await _profileResolver.GetOrCreateCandidateAsync(user);
            var identityChanged = profile.FirstName != model.FirstName || profile.LastName != model.LastName || profile.Phone != model.Phone;
            var saved = await SaveAsync(profile, model);

            // Prénom, nom ou téléphone changés : le compte de connexion les reprend (sans bloquer l'enregistrement de la fiche)
            if (saved != null && identityChanged)
            {
                try
                {
                    await _accountClient.UpdateOwnAccountAsync(saved.FirstName, saved.LastName, saved.Phone);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Compte de connexion du candidat {ProfileId} non mis à jour.", profile.Id);
                }
            }
            return saved;
        }

        public async Task<CandidateProfileViewModel?> UpdateCandidateProfileAsync(int id, AdminCandidateProfileRequest model)
        {
            var profile = await _candidateService.GetCandidateProfileByIdAsync(id);
            if (profile == null) return null;

            // Compte de connexion d'abord : un email déjà pris arrête l'enregistrement
            if (profile.UserId != null)
                await _accountClient.UpdateAccountAsync(profile.UserId, new AccountIdentity(model.FirstName?.Trim(), model.LastName?.Trim(), model.Email!.Trim(), model.Phone?.Trim()));

            profile.Email = model.Email!.Trim();
            return await SaveAsync(profile, model);
        }

        public async Task<CandidateProfileViewModel?> AddCandidateProfileAsync(ProfileCreateRequest model)
        {
            if (await _candidateService.GetCandidateProfileByUserIdAsync(model.UserId!) != null) return null;

            var saved = await _candidateService.AddCandidateProfileAsync(new CandidateProfile
            {
                UserId = model.UserId,
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.Email,
                Situation = CandidateSituations.EnRecherche,
            });
            return _mapper.Map<CandidateProfileViewModel>(saved);
        }

        public async Task<bool> DeleteCandidateProfileAsync(int id)
        {
            var profile = await _candidateService.GetCandidateProfileByIdAsync(id);
            if (profile == null) return false;

            // Copies de CV jointes à ses candidatures : supprimées avec elles
            var applicationCvs = (await _applicationService.GetApplicationCvPathsAsync(candidateProfileId: id)).ToList();
            // CV envoyés dans la messagerie : supprimés avec ses conversations
            applicationCvs.AddRange(await _conversationService.GetAttachmentPathsAsync(candidateProfileId: id, recruiterProfileId: null));
            if (!await _candidateService.DeleteCandidateProfileAsync(id)) return false;

            _fileStorage.Delete(profile.PhotoPath, FileCategory.Photo);
            _fileStorage.Delete(profile.CvPath, FileCategory.Cv);
            applicationCvs.ForEach(path => _fileStorage.Delete(path, FileCategory.Cv));
            return true;
        }

        public async Task<CandidateProfileViewModel> UpdateMyPhotoAsync(ClaimsPrincipal user, IFormFile file)
        {
            var profile = await _profileResolver.GetOrCreateCandidateAsync(user);
            var previous = profile.PhotoPath;
            profile.PhotoPath = await _fileStorage.SaveAsync(file, FileCategory.Photo);
            var saved = await _candidateService.UpdateCandidateProfileAsync(profile);

            // Ancienne photo effacée une fois la nouvelle enregistrée
            _fileStorage.Delete(previous, FileCategory.Photo);
            return _mapper.Map<CandidateProfileViewModel>(saved);
        }

        public async Task<CandidateProfileViewModel> DeleteMyPhotoAsync(ClaimsPrincipal user)
        {
            var profile = await _profileResolver.GetOrCreateCandidateAsync(user);
            _fileStorage.Delete(profile.PhotoPath, FileCategory.Photo);
            profile.PhotoPath = null;
            return _mapper.Map<CandidateProfileViewModel>(await _candidateService.UpdateCandidateProfileAsync(profile));
        }

        public async Task<CandidateProfileViewModel> UpdateMyCvAsync(ClaimsPrincipal user, IFormFile file)
        {
            var profile = await _profileResolver.GetOrCreateCandidateAsync(user);
            var previous = profile.CvPath;
            profile.CvPath = await _fileStorage.SaveAsync(file, FileCategory.Cv);
            profile.CvFileName = Path.GetFileName(file.FileName);
            var saved = await _candidateService.UpdateCandidateProfileAsync(profile);

            // Ancien CV effacé une fois le nouveau enregistré (les copies jointes aux candidatures restent)
            _fileStorage.Delete(previous, FileCategory.Cv);
            return _mapper.Map<CandidateProfileViewModel>(saved);
        }

        public async Task<CandidateProfileViewModel> DeleteMyCvAsync(ClaimsPrincipal user)
        {
            var profile = await _profileResolver.GetOrCreateCandidateAsync(user);
            _fileStorage.Delete(profile.CvPath, FileCategory.Cv);
            profile.CvPath = null;
            profile.CvFileName = null;
            return _mapper.Map<CandidateProfileViewModel>(await _candidateService.UpdateCandidateProfileAsync(profile));
        }

        public async Task<CandidateProfileViewModel> SetMyCvHiddenAsync(ClaimsPrincipal user, bool hidden)
        {
            var profile = await _profileResolver.GetOrCreateCandidateAsync(user);
            if (profile.IsCvHidden == hidden) return _mapper.Map<CandidateProfileViewModel>(profile);
            profile.IsCvHidden = hidden;
            return _mapper.Map<CandidateProfileViewModel>(await _candidateService.UpdateCandidateProfileAsync(profile));
        }

        public async Task<(Stream Stream, string FileName, string ContentType)?> GetCvAsync(int candidateId, ClaimsPrincipal viewer)
        {
            var profile = await _candidateService.GetCandidateProfileByIdAsync(candidateId);
            if (profile?.CvPath == null) return null;

            var isSelf = profile.UserId == viewer.GetUserId();
            if (!isSelf && !viewer.IsRecruteur() && !viewer.HasPermission(Permissions.Candidates.View))
                throw new UnauthorizedAccessException();

            // CV masqué par le candidat : seuls lui et l'administration y accèdent depuis la fiche
            if (profile.IsCvHidden && !isSelf && !viewer.HasPermission(Permissions.Candidates.View)) return null;
            if (profile.IsTestAccount && !isSelf && !viewer.SeesTestCandidates()) return null;

            if (!isSelf) await EnsureFreeRecruiterCanViewAsync(await _profileResolver.FindRecruiterAsync(viewer), profile.Id, viewer);

            // Mêmes règles que la fiche : profil invisible, CV réservé aux recruteurs déjà en relation avec le candidat
            if (profile.IsHidden == true && !isSelf && !viewer.HasPermission(Permissions.Candidates.View))
            {
                var recruiter = await _profileResolver.FindRecruiterAsync(viewer);
                if (recruiter == null || !await _candidateService.HasRelationWithRecruiterAsync(profile.Id, recruiter.Id)) return null;
            }

            var stream = _fileStorage.OpenRead(profile.CvPath, FileCategory.Cv);
            if (stream == null) return null;

            var fileName = profile.CvFileName ?? Path.GetFileName(profile.CvPath);
            return (stream, fileName, _fileStorage.GetContentType(profile.CvPath));
        }

        private async Task<CandidateProfileViewModel?> SaveAsync(CandidateProfile profile, CandidateProfileRequest model)
        {
            if (model.Situation != null && !CandidateSituations.All.Contains(model.Situation))
                throw new ArgumentException("Situation invalide.");
            if (model.AvailableFrom > DateOnly.FromDateTime(DateTime.Today).AddYears(1).AddDays(1))
                throw new ArgumentException("La disponibilité ne peut pas dépasser un an.");

            NormalizeWishes(model);
            if (model.DesiredProfessionId.HasValue)
            {
                var profession = await _professionService.GetProfessionByIdAsync(model.DesiredProfessionId.Value)
                    ?? throw new ArgumentException("Le métier sélectionné n'existe pas.");
                if (model.DesiredDomainId.HasValue && profession.DomainId != model.DesiredDomainId)
                    throw new ArgumentException("Le métier ne correspond pas au domaine sélectionné.");
                model.DesiredDomainId = profession.DomainId;
            }

            // Autres métiers recherchés : même domaine que le premier
            foreach (var professionId in model.WishProfessionIds.Skip(1))
            {
                var profession = await _professionService.GetProfessionByIdAsync(professionId)
                    ?? throw new ArgumentException("Un des métiers recherchés n'existe pas.");
                if (profession.DomainId != model.DesiredDomainId)
                    throw new ArgumentException("Les métiers recherchés doivent appartenir au domaine sélectionné.");
            }

            foreach (var experience in model.Experiences)
            {
                if (experience.IsCurrent != true && experience.StartDate > experience.EndDate)
                    throw new ArgumentException($"Expérience « {experience.Title} » : la date de fin précède la date de début.");
            }

            // Les champs absents du formulaire (photo, CV, email, utilisateur) sont conservés
            _mapper.Map(model, profile);
            var saved = await _candidateService.UpdateCandidateProfileAsync(profile);
            return saved == null ? null : _mapper.Map<CandidateProfileViewModel>(saved);
        }

        // Vœux : listes dédoublonnées et bornées ; le premier métier recherché est aussi « le » métier du profil
        // (correspondance côté recruteur). Un ancien client qui n'envoie que DesiredProfessionId garde ce métier.
        private static void NormalizeWishes(CandidateProfileRequest model)
        {
            if (model.WishProfessionIds.Count == 0 && model.DesiredProfessionId.HasValue)
                model.WishProfessionIds = [model.DesiredProfessionId.Value];
            model.WishProfessionIds = model.WishProfessionIds.Distinct().ToList();
            if (model.WishProfessionIds.Count > MaxWishItems)
                throw new ArgumentException($"{MaxWishItems} métiers recherchés au maximum.");
            model.DesiredProfessionId = model.WishProfessionIds.Count > 0 ? model.WishProfessionIds[0] : null;

            model.WishContractTypes = model.WishContractTypes.Distinct().ToList();
            if (model.WishContractTypes.Any(c => !ContractTypes.All.Contains(c)))
                throw new ArgumentException("Type de contrat recherché invalide.");

            model.WishCities = model.WishCities
                .Where(c => !string.IsNullOrWhiteSpace(c)).Select(c => c.Trim())
                .DistinctBy(c => c.ToLowerInvariant()).ToList();
            if (model.WishCities.Count > MaxWishItems)
                throw new ArgumentException($"{MaxWishItems} villes au maximum.");
            if (model.WishCities.Any(c => c.Length > 100))
                throw new ArgumentException("Nom de ville trop long.");
            if (model.WishAnywhere) model.WishCities = [];
        }

        private const int MaxWishItems = 5;
    }
}
