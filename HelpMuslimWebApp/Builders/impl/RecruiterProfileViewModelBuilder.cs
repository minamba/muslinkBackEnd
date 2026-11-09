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
    public class RecruiterProfileViewModelBuilder : IRecruiterProfileViewModelBuilder
    {
        private readonly IMapper _mapper;
        private readonly IRecruiterProfileService _recruiterService;
        private readonly IProfileResolver _profileResolver;
        private readonly IFileStorageService _fileStorage;
        private readonly IJobApplicationService _applicationService;
        private readonly IConversationService _conversationService;
        private readonly IIdentityAccountClient _accountClient;
        private readonly IMatchAlertService _matchAlertService;
        private readonly IFounderViewModelBuilder _founderBuilder;
        private readonly IFounderService _founderService;
        private readonly TestModeGuard _testMode;
        private readonly ILogger<RecruiterProfileViewModelBuilder> _logger;

        public RecruiterProfileViewModelBuilder(
            IMapper mapper,
            IRecruiterProfileService recruiterService,
            IProfileResolver profileResolver,
            IFileStorageService fileStorage,
            IJobApplicationService applicationService,
            IConversationService conversationService,
            IIdentityAccountClient accountClient,
            IMatchAlertService matchAlertService,
            IFounderViewModelBuilder founderBuilder,
            IFounderService founderService,
            TestModeGuard testMode,
            ILogger<RecruiterProfileViewModelBuilder> logger)
        {
            _mapper = mapper;
            _recruiterService = recruiterService;
            _profileResolver = profileResolver;
            _fileStorage = fileStorage;
            _applicationService = applicationService;
            _conversationService = conversationService;
            _accountClient = accountClient;
            _matchAlertService = matchAlertService;
            _founderBuilder = founderBuilder;
            _founderService = founderService;
            _testMode = testMode;
            _logger = logger;
        }

        public async Task<IEnumerable<RecruiterProfileViewModel>> GetRecruiterProfilesAsync(string? search) =>
            _mapper.Map<IEnumerable<RecruiterProfileViewModel>>(await _recruiterService.GetRecruiterProfilesAsync(search));

        public async Task<RecruiterProfileViewModel?> GetRecruiterProfileByIdAsync(int id, ClaimsPrincipal viewer)
        {
            var profile = await _recruiterService.GetRecruiterProfileByIdAsync(id);
            if (profile == null) return null;

            // Entreprise désactivée : invisible de tous, sauf du recruteur lui-même et de l'administration
            var isSelf = profile.UserId == viewer.GetUserId();
            if (profile.IsDisabled && !isSelf && !viewer.HasPermission(Permissions.Recruiters.View) && !viewer.HasPermission(Permissions.Companies.View))
                return null;
            // Recruteur de test : introuvable pour les vrais comptes
            if (profile.IsTestAccount && !isSelf && !viewer.SeesTestRecruiters()) return null;

            var viewModel = _mapper.Map<RecruiterProfileViewModel>(profile);

            // Fiche entreprise publique : coordonnées réservées au recruteur lui-même et à l'administration
            if (!isSelf && !viewer.HasPermission(Permissions.Recruiters.View))
            {
                viewModel.Email = null;
                viewModel.Phone = null;
                viewModel.UserId = null;
                viewModel.IsPro = false;
                viewModel.ProGrantedUntil = null;
                viewModel.ProPaidUntil = null;
                viewModel.HasProSubscription = false;
                viewModel.ProCancelAtPeriodEnd = false;
                viewModel.MatchAlertsEnabled = false;
                viewModel.MessageAlertsEnabled = false;
                viewModel.ApplicationAlertsEnabled = false;
                viewModel.Siret = null;
                viewModel.Rna = null;
                viewModel.LegalName = null;
                viewModel.IdentityVerified = false;
            }

            return viewModel;
        }

        public async Task<RecruiterProfileViewModel> GetMyProfileAsync(ClaimsPrincipal user) =>
            (await MineAsync(user, _mapper.Map<RecruiterProfileViewModel>(await _profileResolver.GetOrCreateRecruiterAsync(user))))!;

        // Sa propre fiche : vérification de l'entreprise levée pour un compte de test en mode test (le front ne bloque alors
        // ni la publication ni l'accès aux candidats, comme l'Api)
        private async Task<RecruiterProfileViewModel?> MineAsync(ClaimsPrincipal user, RecruiterProfileViewModel? viewModel)
        {
            if (viewModel != null) viewModel.VerificationExempt = await _testMode.ExemptsAsync(user);
            return viewModel;
        }

        public async Task<RecruiterProfileViewModel> SetMyMatchAlertsAsync(ClaimsPrincipal user, bool enabled)
        {
            var profile = await _profileResolver.GetOrCreateRecruiterAsync(user);
            await _matchAlertService.SetEnabledAsync(profile.Id, enabled);
            profile.MatchAlertsEnabled = enabled;
            return (await MineAsync(user, _mapper.Map<RecruiterProfileViewModel>(profile)))!;
        }

        public Task<bool> SetMatchAlertsByTokenAsync(string token, bool enabled) => _matchAlertService.SetEnabledByTokenAsync(token, enabled);

        public async Task<RecruiterProfileViewModel> SetMyMessageAlertsAsync(ClaimsPrincipal user, bool enabled)
        {
            var profile = await _profileResolver.GetOrCreateRecruiterAsync(user);
            await _matchAlertService.SetMessageAlertsEnabledAsync(profile.Id, enabled);
            profile.MessageAlertsEnabled = enabled;
            return (await MineAsync(user, _mapper.Map<RecruiterProfileViewModel>(profile)))!;
        }

        public Task<bool> SetMessageAlertsByTokenAsync(string token, bool enabled) => _matchAlertService.SetMessageAlertsEnabledByTokenAsync(token, enabled);

        public async Task<RecruiterProfileViewModel> SetMyApplicationAlertsAsync(ClaimsPrincipal user, bool enabled)
        {
            var profile = await _profileResolver.GetOrCreateRecruiterAsync(user);
            await _matchAlertService.SetApplicationAlertsEnabledAsync(profile.Id, enabled);
            profile.ApplicationAlertsEnabled = enabled;
            return (await MineAsync(user, _mapper.Map<RecruiterProfileViewModel>(profile)))!;
        }

        public Task<bool> SetApplicationAlertsByTokenAsync(string token, bool enabled) => _matchAlertService.SetApplicationAlertsEnabledByTokenAsync(token, enabled);

        public async Task<RecruiterProfileViewModel?> UpdateMyProfileAsync(ClaimsPrincipal user, RecruiterProfileRequest model)
        {
            var profile = await _profileResolver.GetOrCreateRecruiterAsync(user);
            var identityChanged = profile.FirstName != model.FirstName || profile.LastName != model.LastName || profile.Phone != model.Phone;
            // Nom de la structure et SIRET / RNA vérifiés ensemble avant tout enregistrement (incohérence : rien n'est enregistré)
            var company = await _founderBuilder.ResolveIdentityAsync(profile, model.CompanyName, model.Siret, model.Rna, await _testMode.ExemptsAsync(user));
            var saved = await SaveAsync(profile, model);
            if (saved != null && company != null)
            {
                await _founderService.SetIdentityAsync(profile.Id, company);
                saved = _mapper.Map<RecruiterProfileViewModel>(await _recruiterService.GetRecruiterProfileByIdAsync(profile.Id));
            }
            // Offre de lancement : entreprise tout juste vérifiée, la place de fondatrice peut être attribuée
            if (saved != null && company?.VerifiedAt != null)
            {
                await _founderBuilder.TryGrantFounderAsync(user);
                saved = _mapper.Map<RecruiterProfileViewModel>(await _recruiterService.GetRecruiterProfileByIdAsync(profile.Id));
            }

            // Prénom, nom ou téléphone changés : le compte de connexion les reprend (sans bloquer l'enregistrement de la fiche)
            if (saved != null && identityChanged)
            {
                try
                {
                    await _accountClient.UpdateOwnAccountAsync(saved.FirstName, saved.LastName, saved.Phone);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Compte de connexion du recruteur {ProfileId} non mis à jour.", profile.Id);
                }
            }
            return await MineAsync(user, saved);
        }

        public async Task<RecruiterProfileViewModel?> UpdateRecruiterProfileAsync(int id, AdminRecruiterProfileRequest model)
        {
            var profile = await _recruiterService.GetRecruiterProfileByIdAsync(id);
            if (profile == null) return null;

            // Compte de connexion d'abord : un email déjà pris arrête l'enregistrement
            if (profile.UserId != null)
                await _accountClient.UpdateAccountAsync(profile.UserId, new AccountIdentity(model.FirstName?.Trim(), model.LastName?.Trim(), model.Email!.Trim(), model.Phone?.Trim()));

            profile.Email = model.Email!.Trim();
            return await SaveAsync(profile, model);
        }

        public async Task<RecruiterProfileViewModel?> AddRecruiterProfileAsync(ProfileCreateRequest model)
        {
            if (await _recruiterService.GetRecruiterProfileByUserIdAsync(model.UserId!) != null) return null;

            var saved = await _recruiterService.AddRecruiterProfileAsync(new RecruiterProfile
            {
                UserId = model.UserId,
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.Email,
                CompanyName = model.CompanyName,
            });
            return _mapper.Map<RecruiterProfileViewModel>(saved);
        }

        public async Task<bool> DeleteRecruiterProfileAsync(int id)
        {
            var profile = await _recruiterService.GetRecruiterProfileByIdAsync(id);
            if (profile == null) return false;

            // Offres et candidatures supprimées avec le profil : copies de CV jointes comprises
            var applicationCvs = (await _applicationService.GetApplicationCvPathsAsync(recruiterProfileId: id)).ToList();
            // CV reçus dans la messagerie : supprimés avec ses conversations
            applicationCvs.AddRange(await _conversationService.GetAttachmentPathsAsync(candidateProfileId: null, recruiterProfileId: id));
            // Page entreprise : couverture et photos
            var pageFiles = await _recruiterService.GetCompanyFilePathsAsync(id);
            if (!await _recruiterService.DeleteRecruiterProfileAsync(id)) return false;

            _fileStorage.Delete(profile.LogoPath, FileCategory.Logo);
            pageFiles.ForEach(path => _fileStorage.Delete(path, FileStorageService.CompanyCategoryOf(path)));
            applicationCvs.ForEach(path => _fileStorage.Delete(path, FileCategory.Cv));
            return true;
        }

        public async Task<RecruiterProfileViewModel> UpdateMyLogoAsync(ClaimsPrincipal user, IFormFile file)
        {
            var profile = await _profileResolver.GetOrCreateRecruiterAsync(user);
            var previous = profile.LogoPath;
            profile.LogoPath = await _fileStorage.SaveAsync(file, FileCategory.Logo);
            var saved = await _recruiterService.UpdateRecruiterProfileAsync(profile);

            // Ancien logo effacé une fois le nouveau enregistré
            _fileStorage.Delete(previous, FileCategory.Logo);
            return (await MineAsync(user, _mapper.Map<RecruiterProfileViewModel>(saved)))!;
        }

        public async Task<RecruiterProfileViewModel> DeleteMyLogoAsync(ClaimsPrincipal user)
        {
            var profile = await _profileResolver.GetOrCreateRecruiterAsync(user);
            _fileStorage.Delete(profile.LogoPath, FileCategory.Logo);
            profile.LogoPath = null;
            return (await MineAsync(user, _mapper.Map<RecruiterProfileViewModel>(await _recruiterService.UpdateRecruiterProfileAsync(profile))))!;
        }

        private async Task<RecruiterProfileViewModel?> SaveAsync(RecruiterProfile profile, RecruiterProfileRequest model)
        {
            _mapper.Map(model, profile);
            var saved = await _recruiterService.UpdateRecruiterProfileAsync(profile);
            return saved == null ? null : _mapper.Map<RecruiterProfileViewModel>(saved);
        }
    }
}
