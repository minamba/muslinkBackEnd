using System.Security.Claims;
using AutoMapper;
using HelpMuslimWebApp.Api.Request;
using HelpMuslimWebApp.Api.Services;
using HelpMuslimWebApp.Api.Services.Demo;
using HelpMuslimWebApp.Api.Services.Seo;
using HelpMuslimWebApp.Api.Services.Telegram;
using HelpMuslimWebApp.Api.Utils;
using HelpMuslimWebApp.Api.ViewModels;
using HelpMuslimWebApp.Domain.Models;
using HelpMuslimWebApp.Domain.Security;
using HelpMuslimWebApp.Domain.Services;

namespace HelpMuslimWebApp.Api.Builders.impl
{
    public class JobOfferViewModelBuilder : IJobOfferViewModelBuilder
    {
        private readonly IMapper _mapper;
        private readonly IPremiumService _premiumService;
        private readonly TestModeGuard _testMode;
        private readonly IJobOfferService _jobOfferService;
        private readonly IJobDomainService _jobDomainService;
        private readonly IProfessionService _professionService;
        private readonly IRecruiterProfileService _recruiterService;
        private readonly IJobApplicationService _applicationService;
        private readonly IProfileResolver _profileResolver;
        private readonly IRealtimeNotifier _notifier;
        private readonly IFounderViewModelBuilder _founderBuilder;
        private readonly IEmailService _emailService;
        private readonly IFileStorageService _fileStorage;
        private readonly ITelegramNotifier _telegram;
        private readonly ILogger<JobOfferViewModelBuilder> _logger;
        private readonly GoogleIndexingQueue _indexing;
        private readonly IDemoContext _demo;
        private readonly ICompanyDigestService _digests;

        public JobOfferViewModelBuilder(
            IMapper mapper,
            IPremiumService premiumService,
            TestModeGuard testMode,
            IJobOfferService jobOfferService,
            IJobDomainService jobDomainService,
            IProfessionService professionService,
            IRecruiterProfileService recruiterService,
            IJobApplicationService applicationService,
            IProfileResolver profileResolver,
            IRealtimeNotifier notifier,
            IFounderViewModelBuilder founderBuilder,
            IEmailService emailService,
            IFileStorageService fileStorage,
            ITelegramNotifier telegram,
            ILogger<JobOfferViewModelBuilder> logger,
            GoogleIndexingQueue indexing,
            IDemoContext demo,
            ICompanyDigestService digests)
        {
            _digests = digests;
            _indexing = indexing;
            _demo = demo;
            _telegram = telegram;
            _notifier = notifier;
            _founderBuilder = founderBuilder;
            _emailService = emailService;
            _fileStorage = fileStorage;
            _logger = logger;
            _mapper = mapper;
            _premiumService = premiumService;
            _testMode = testMode;
            _jobOfferService = jobOfferService;
            _jobDomainService = jobDomainService;
            _professionService = professionService;
            _recruiterService = recruiterService;
            _applicationService = applicationService;
            _profileResolver = profileResolver;
        }

        // Google for Jobs : l'offre a changé, Google en est prévenu (jamais pour la démo, dont les offres ne sont pas publiques)
        private void OfferChanged(int offerId)
        {
            if (!_demo.IsDemo) _indexing.OfferChanged(offerId);
        }

        // Fil d'actualité public : uniquement les offres ouvertes.
        // Candidat connecté : offres classées de la plus proche à la plus éloignée de son profil.
        public async Task<PagedViewModel<JobOfferViewModel>> GetJobOffersAsync(OfferQueryRequest query, ClaimsPrincipal user)
        {
            var candidate = user.IsCandidat() ? await _profileResolver.FindCandidateAsync(user) : null;
            if (candidate == null) return await SearchAsync(query, onlyOpen: true, user.SeesTestRecruiters());

            var filter = ToFilter(query, onlyOpen: true, user.SeesTestRecruiters());
            var page = Math.Max(1, query.Page);
            var pageSize = Math.Clamp(query.PageSize, 1, 50);

            var offers = await _jobOfferService.GetFilteredJobOffersAsync(filter);
            // Offres sponsorisées en tête (tri stable : la correspondance ordonne chaque groupe)
            var ranked = CandidateMatcher.RankOffers(ToSummary(candidate), offers).OrderByDescending(m => m.Offer.IsSponsored).ToList();

            var items = ranked.Skip((page - 1) * pageSize).Take(pageSize).Select(m =>
            {
                var viewModel = _mapper.Map<JobOfferViewModel>(m.Offer);
                viewModel.MatchScore = m.Score;
                viewModel.MatchReasons = m.Reasons;
                viewModel.MatchedSkills = m.MatchedSkills;
                return viewModel;
            }).ToList();

            return new PagedViewModel<JobOfferViewModel> { Items = items, Total = ranked.Count, Page = page, PageSize = pageSize };
        }

        // Administration : toutes les offres, y compris fermées
        public Task<PagedViewModel<JobOfferViewModel>> GetAllJobOffersAsync(OfferQueryRequest query) => SearchAsync(query, onlyOpen: false, includeTestAccounts: true);

        // Candidat : offres ouvertes qui correspondent à ses vœux à 70 % au moins, hors offres auxquelles il a déjà postulé.
        // Premium candidats activé, candidat gratuit : aperçu d'une seule offre, les autres verrouillées (score seul, sans
        // titre, entreprise ni lien) ; il reste toujours au moins une offre verrouillée (une seule correspondance : verrouillée).
        public async Task<WishOffersViewModel> GetWishOffersAsync(ClaimsPrincipal user, int? take)
        {
            var result = new WishOffersViewModel { Threshold = WishMatcher.Threshold };
            var candidate = await _profileResolver.FindCandidateAsync(user);
            if (candidate == null || !WishMatcher.HasWishes(candidate)) return result;
            result.HasWishes = true;

            var professionNames = new List<string>();
            foreach (var professionId in candidate.WishProfessionIds)
            {
                if (await _professionService.GetProfessionByIdAsync(professionId) is { Name: { } name }) professionNames.Add(name);
            }

            var applied = (await _applicationService.GetCandidateApplicationsAsync(candidate.Id)).Select(a => a.JobOfferId).ToHashSet();
            var offers = await _jobOfferService.GetFilteredJobOffersAsync(ToFilter(new OfferQueryRequest(), onlyOpen: true, user.SeesTestRecruiters()));
            var matches = offers
                .Where(o => !applied.Contains(o.Id))
                .Select(o => WishMatcher.Score(candidate, professionNames, o))
                .Where(m => m.Score >= WishMatcher.Threshold)
                .OrderByDescending(m => m.Score)
                .ThenByDescending(m => m.Offer.CreatedAt)
                .ToList();

            result.Total = matches.Count;
            var settings = await _premiumService.GetSettingsAsync();
            result.PremiumRequired = settings.CandidatesEnabled && !Premium.IsActive(candidate.PremiumUntil) && !await _testMode.ExemptsAsync(user);
            var visible = result.PremiumRequired ? (matches.Count >= 2 ? 1 : 0) : int.MaxValue;

            result.Items = matches.Take(take is > 0 ? take.Value : int.MaxValue).Select((m, index) =>
            {
                if (index >= visible) return new JobOfferViewModel { IsLocked = true, WishScore = m.Score };

                var viewModel = _mapper.Map<JobOfferViewModel>(m.Offer);
                viewModel.WishScore = m.Score;
                viewModel.WishCriteria = m.Criteria
                    .Select(c => new WishCriterionViewModel { Label = c.Label, Status = c.Credit >= 1 ? "ok" : c.Credit > 0 ? "partial" : "ko" })
                    .ToList();
                return viewModel;
            }).ToList();
            return result;
        }

        public async Task<IEnumerable<JobOfferViewModel>> GetMyJobOffersAsync(ClaimsPrincipal user)
        {
            var recruiter = await _profileResolver.GetOrCreateRecruiterAsync(user);
            var result = await _jobOfferService.GetJobOffersAsync(new OfferFilter
            {
                RecruiterProfileId = recruiter.Id,
                OnlyOpen = false,
                PageSize = 50,
            });

            var viewModels = _mapper.Map<List<JobOfferViewModel>>(result.Items);
            viewModels.ForEach(o => o.IsOwner = true);
            return viewModels;
        }

        public async Task<JobOfferViewModel?> GetJobOfferByIdAsync(int id, ClaimsPrincipal user)
        {
            var offer = await _jobOfferService.GetJobOfferByIdAsync(id);
            if (offer == null) return null;

            var viewModel = _mapper.Map<JobOfferViewModel>(offer);
            viewModel.IsOwner = offer.RecruiterUserId != null && offer.RecruiterUserId == user.GetUserId();

            var candidate = await _profileResolver.FindCandidateAsync(user);
            if (candidate != null)
            {
                viewModel.HasApplied = await _applicationService.JobApplicationExistsAsync(offer.Id, candidate.Id);
                var match = CandidateMatcher.RankOffers(ToSummary(candidate), [offer])[0];
                viewModel.MatchScore = match.Score;
                viewModel.MatchReasons = match.Reasons;
                viewModel.MatchedSkills = match.MatchedSkills;
            }

            // Entreprise désactivée : ses offres ne sont plus visibles de personne, hormis son recruteur et l'administration
            if (offer.CompanyDisabled && !viewModel.IsOwner && !user.HasPermission(Permissions.Offers.View))
                return null;

            // Recruteur de test : offre introuvable pour les vrais comptes
            if (offer.RecruiterIsTestAccount && !viewModel.IsOwner && !user.SeesTestRecruiters())
                return null;

            // Offre fermée : invisible pour les candidats, sauf ceux qui y ont postulé
            if ((offer.IsOpen != true || offer.IsPubliclyHidden) && !viewModel.IsOwner && !viewModel.HasApplied && !user.HasPermission(Permissions.Offers.View))
                return null;

            // Statistiques : une consultation de plus (hors recruteur propriétaire et administration)
            if (!viewModel.IsOwner && !user.HasPermission(Permissions.Offers.View))
            {
                try
                {
                    await _jobOfferService.IncrementViewsAsync(offer.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Consultation de l'offre {OfferId} non comptée.", offer.Id);
                }
            }

            return viewModel;
        }

        public async Task<JobOfferViewModel> AddJobOfferAsync(ClaimsPrincipal user, JobOfferRequest model)
        {
            await ValidateAsync(model);

            int recruiterProfileId;
            if (user.IsRecruteur())
            {
                var self = await _profileResolver.GetOrCreateRecruiterAsync(user);
                if (self.IsDisabled)
                    throw new InvalidOperationException("Votre entreprise a été désactivée : vous ne pouvez plus publier d'offre. Contactez le support si vous pensez qu'il s'agit d'une erreur.");
                if (!await _testMode.ExemptsAsync(user))
                {
                    CompanyVerification.Ensure(self, CompanyVerification.PublishMessage);
                    await EnsureCanPublishAsync(self, model.ContractType, exceptOfferId: null);
                }
                recruiterProfileId = self.Id;
            }
            else if (user.HasPermission(Permissions.Offers.Create) && model.RecruiterProfileId.HasValue)
            {
                var recruiter = await _recruiterService.GetRecruiterProfileByIdAsync(model.RecruiterProfileId.Value)
                    ?? throw new ArgumentException("Le recruteur sélectionné n'existe pas.");
                recruiterProfileId = recruiter.Id;
            }
            else
            {
                throw new UnauthorizedAccessException();
            }

            var offer = _mapper.Map<JobOffer>(model);
            offer.RecruiterProfileId = recruiterProfileId;

            var saved = await _jobOfferService.AddJobOfferAsync(offer);
            await _telegram.NewOfferAsync(saved);
            OfferChanged(saved.Id);
            if (!saved.IsPubliclyHidden)
            {
                // Un seul email par candidat (celui des vœux prime sur celui de l'entreprise suivie)
                var alerted = await NotifyWishMatchesAsync(saved);
                await NotifyCompanyFollowersAsync(saved, alerted);
            }
            // Offre de lancement : première offre publiée, dernière condition d'une place d'entreprise fondatrice
            if (user.IsRecruteur()) await _founderBuilder.TryGrantFounderAsync(user);

            var viewModel = _mapper.Map<JobOfferViewModel>(saved);
            viewModel.IsOwner = user.IsRecruteur();
            return viewModel;
        }

        // Recruteur Pro activé : un recruteur gratuit garde au plus 3 offres ouvertes hors bénévolat.
        // Les offres déjà en ligne au moment de l'activation restent publiées ; la limite vaut pour les nouvelles publications.
        private async Task EnsureCanPublishAsync(RecruiterProfile recruiter, string? contractType, int? exceptOfferId)
        {
            if (contractType == ContractTypes.Benevolat || Premium.IsActive(recruiter.ProUntil)) return;
            if (!(await _premiumService.GetSettingsAsync()).RecruitersEnabled) return;

            var active = await _premiumService.CountActiveOffersAsync(recruiter.Id, exceptOfferId);
            if (active >= Premium.FreeActiveOffers)
                throw new InvalidOperationException($"Vous avez déjà {active} offres ouvertes : l'offre gratuite en permet {Premium.FreeActiveOffers} à la fois. Fermez une offre ou passez à Recruteur Pro pour publier sans limite.");
        }

        // Email aux candidats dont les vœux correspondent à l'offre à 70 % au moins : les abonnés premium quand le mode premium
        // est actif, tous les candidats quand il est coupé. Retourne les candidats prévenus (une erreur n'empêche pas la publication).
        private async Task<HashSet<int>> NotifyWishMatchesAsync(JobOffer offer)
        {
            var alerted = new HashSet<int>();
            try
            {
                var premiumEnabled = (await _premiumService.GetSettingsAsync()).CandidatesEnabled;

                var professionNames = new Dictionary<int, string>();
                foreach (var candidate in await _premiumService.GetWishAlertProfilesAsync(premiumOnly: premiumEnabled))
                {
                    if (string.IsNullOrWhiteSpace(candidate.Email) || !WishMatcher.HasWishes(candidate)) continue;
                    // Recruteur de test : seuls les candidats de test sont prévenus
                    if (offer.RecruiterIsTestAccount && !candidate.IsTestAccount) continue;

                    var names = new List<string>();
                    foreach (var id in candidate.WishProfessionIds)
                    {
                        if (!professionNames.TryGetValue(id, out var name))
                            professionNames[id] = name = (await _professionService.GetProfessionByIdAsync(id))?.Name ?? string.Empty;
                        if (name.Length > 0) names.Add(name);
                    }

                    var match = WishMatcher.Score(candidate, names, offer);
                    if (match.Score < WishMatcher.Threshold) continue;

                    await _emailService.SendWishOfferEmailAsync(candidate.Email, candidate.FirstName ?? string.Empty, offer.CompanyName ?? "Une entreprise",
                        offer.Title ?? string.Empty, match.Score, match.Criteria.Where(c => c.Credit >= 1).Select(c => c.Label).ToList(), offer.Id, premiumEnabled);
                    alerted.Add(candidate.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Alertes « selon vos vœux » de l'offre {OfferId} impossibles.", offer.Id);
            }
            return alerted;
        }

        // Candidats qui suivent l'entreprise : notification en direct pour tous ; email aux candidats premium quand le mode
        // premium est actif, à tous quand il est coupé (sauf ceux déjà prévenus par l'alerte de leurs vœux). Mode premium actif :
        // l'offre entre dans le récapitulatif quotidien des candidats gratuits. Une erreur n'empêche pas la publication.
        private async Task NotifyCompanyFollowersAsync(JobOffer offer, HashSet<int> alreadyEmailed)
        {
            bool? premiumEnabled = null;
            try
            {
                premiumEnabled = (await _premiumService.GetSettingsAsync()).CandidatesEnabled;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Mode premium illisible : pas d'email aux abonnés de l'entreprise.");
            }

            IEnumerable<CompanyFollower> followers;
            try
            {
                followers = await _recruiterService.GetCompanyFollowersAsync(offer.RecruiterProfileId!.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Abonnés de l'entreprise {RecruiterProfileId} introuvables.", offer.RecruiterProfileId);
                return;
            }

            var companyName = offer.CompanyName ?? "Une entreprise";
            var notification = new CompanyOfferNotificationViewModel
            {
                OfferId = offer.Id,
                Title = offer.Title,
                City = offer.City,
                ContractType = offer.ContractType,
                CompanyId = offer.RecruiterProfileId!.Value,
                CompanyName = companyName,
                CompanyLogoUrl = offer.CompanyLogoPath,
            };
            var details = string.Join(" · ", new[] { ContractTypes.Label(offer.ContractType), offer.City }.Where(s => !string.IsNullOrWhiteSpace(s)));
            var digest = new List<int>();

            foreach (var follower in followers)
            {
                // Recruteur de test : seuls les candidats de test sont prévenus
                if (offer.RecruiterIsTestAccount && !follower.IsTestAccount) continue;
                try
                {
                    if (!string.IsNullOrEmpty(follower.UserId)) await _notifier.CompanyOfferPublishedAsync(follower.UserId, notification);
                    if (alreadyEmailed.Contains(follower.CandidateProfileId) || string.IsNullOrWhiteSpace(follower.Email)) continue;
                    // Mode premium illisible (null) : ni email, ni récapitulatif
                    if (premiumEnabled == false || (premiumEnabled == true && Premium.IsActive(follower.PremiumUntil)))
                        await _emailService.SendNewCompanyOfferEmailAsync(follower.Email, follower.FirstName ?? string.Empty, companyName, offer.Title ?? string.Empty, details, offer.Id);
                    else if (premiumEnabled == true)
                        digest.Add(follower.CandidateProfileId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Alerte de nouvelle offre {OfferId} au candidat {CandidateId} impossible.", offer.Id, follower.CandidateProfileId);
                }
            }

            try
            {
                await _digests.AddPendingAsync(offer.Id, digest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Offre {OfferId} : récapitulatif des abonnés gratuits impossible.", offer.Id);
            }
        }

        public async Task<JobOfferViewModel?> UpdateJobOfferAsync(ClaimsPrincipal user, JobOfferRequest model)
        {
            var existing = await _jobOfferService.GetJobOfferByIdAsync(model.Id);
            if (existing == null) return null;
            EnsureCanManage(user, existing, Permissions.Offers.Edit);

            // Offre ouverte qui quitte le bénévolat : elle entre dans la limite de l'offre gratuite
            if (user.IsRecruteur() && existing.IsOpen == true && existing.ContractType == ContractTypes.Benevolat && model.ContractType != ContractTypes.Benevolat
                && !await _testMode.ExemptsAsync(user))
                await EnsureCanPublishAsync(await _profileResolver.GetOrCreateRecruiterAsync(user), model.ContractType, existing.Id);

            await ValidateAsync(model);
            var offer = _mapper.Map<JobOffer>(model);
            var saved = await _jobOfferService.UpdateJobOfferAsync(offer);
            if (saved != null) OfferChanged(saved.Id);
            return saved == null ? null : WithOwner(saved, user);
        }

        public async Task<JobOfferViewModel?> SetJobOfferStatusAsync(ClaimsPrincipal user, int id, bool isOpen, bool isFilled, int? hiredCandidateProfileId = null)
        {
            var existing = await _jobOfferService.GetJobOfferByIdAsync(id);
            if (existing == null) return null;
            EnsureCanManage(user, existing, Permissions.Offers.Edit);
            if (isOpen) EnsureCompanyActive(user, existing, "rouvrir cette offre");
            if (isOpen && existing.IsOpen != true && user.IsRecruteur())
            {
                var self = await _profileResolver.GetOrCreateRecruiterAsync(user);
                if (!await _testMode.ExemptsAsync(user))
                {
                    CompanyVerification.Ensure(self, CompanyVerification.PublishMessage);
                    await EnsureCanPublishAsync(self, existing.ContractType, existing.Id);
                }
            }

            var saved = await _jobOfferService.SetJobOfferOpenAsync(id, isOpen, isFilled);
            if (saved == null) return null;
            OfferChanged(id);

            // Offre pourvue : le compteur de l'accueil se met à jour en direct chez tous les visiteurs
            if (saved.IsFilled == true)
            {
                try
                {
                    await _notifier.OfferFilledAsync(await GetFilledOffersAsync(10));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Diffusion temps réel de l'offre pourvue {OfferId} impossible.", id);
                }

                // Vient d'être pourvue : les candidats non retenus sont prévenus par email
                if (existing.IsFilled != true) await NotifyUnsuccessfulApplicantsAsync(saved, hiredCandidateProfileId);
            }
            return WithOwner(saved, user);
        }

        // Offre pourvue : email à chaque candidat qui avait postulé, sauf le candidat retenu
        private async Task NotifyUnsuccessfulApplicantsAsync(JobOffer offer, int? hiredCandidateProfileId)
        {
            IEnumerable<JobApplication> applications;
            try
            {
                applications = await _applicationService.GetJobApplicationsAsync(jobOfferId: offer.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Candidatures de l'offre pourvue {OfferId} introuvables.", offer.Id);
                return;
            }

            foreach (var application in applications.Where(a => a.CandidateProfileId != hiredCandidateProfileId))
            {
                var email = application.Candidate?.Email;
                if (string.IsNullOrWhiteSpace(email)) continue;
                try
                {
                    await _emailService.SendOfferFilledEmailAsync(email, application.Candidate!.FirstName ?? string.Empty,
                        offer.CompanyName ?? "Le recruteur", offer.Title ?? string.Empty);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Email « offre pourvue » {OfferId} au candidat {CandidateId} impossible.", offer.Id, application.CandidateProfileId);
                }
            }
        }

        public Task<int> DeleteOfferFillsAsync(int recruiterProfileId) => _jobOfferService.DeleteOfferFillsAsync(recruiterProfileId);

        // ---------- Offres favorites du candidat ----------

        public async Task<IEnumerable<JobOfferViewModel>> GetFavoriteOffersAsync(ClaimsPrincipal user)
        {
            var candidate = await _profileResolver.GetOrCreateCandidateAsync(user);
            var favorites = (await _jobOfferService.GetFavoriteOffersAsync(candidate.Id)).Where(o => !o.RecruiterIsTestAccount || user.SeesTestRecruiters());
            return _mapper.Map<List<JobOfferViewModel>>(favorites);
        }

        public async Task<bool> AddFavoriteOfferAsync(ClaimsPrincipal user, int offerId)
        {
            var offer = await _jobOfferService.GetJobOfferByIdAsync(offerId);
            if (offer == null || offer.IsPubliclyHidden || (offer.RecruiterIsTestAccount && !user.SeesTestRecruiters())) return false;
            // Seules les offres ouvertes peuvent être ajoutées (une offre déjà en favori le reste à sa fermeture)
            if (offer.IsOpen != true) throw new InvalidOperationException("Cette offre est fermée, elle ne peut plus être ajoutée aux favoris.");

            var candidate = await _profileResolver.GetOrCreateCandidateAsync(user);
            await _jobOfferService.AddOfferFavoriteAsync(candidate.Id, offerId);
            return true;
        }

        public async Task<bool> RemoveFavoriteOfferAsync(ClaimsPrincipal user, int offerId)
        {
            var candidate = await _profileResolver.GetOrCreateCandidateAsync(user);
            return await _jobOfferService.DeleteOfferFavoriteAsync(candidate.Id, offerId);
        }

        public async Task<JobOfferViewModel?> SetJobOfferVisibilityAsync(ClaimsPrincipal user, int id, bool isHidden)
        {
            var existing = await _jobOfferService.GetJobOfferByIdAsync(id);
            if (existing == null) return null;
            EnsureCanManage(user, existing, Permissions.Offers.Edit);
            if (!isHidden) EnsureCompanyActive(user, existing, "rendre cette offre visible");

            var saved = await _jobOfferService.SetJobOfferHiddenAsync(id, isHidden);
            if (saved != null) OfferChanged(id);
            return saved == null ? null : WithOwner(saved, user);
        }

        public async Task<FilledOffersViewModel> GetFilledOffersAsync(int take) =>
            _mapper.Map<FilledOffersViewModel>(await _jobOfferService.GetFilledOffersStatsAsync(take));

        public async Task<bool> DeleteJobOfferAsync(ClaimsPrincipal user, int id)
        {
            var existing = await _jobOfferService.GetJobOfferByIdAsync(id);
            if (existing == null) return false;
            EnsureCanManage(user, existing, Permissions.Offers.Delete);

            // Les candidatures partent avec l'offre : leurs copies de CV aussi
            var applicationCvs = (await _applicationService.GetApplicationCvPathsAsync(jobOfferId: id)).ToList();
            if (!await _jobOfferService.DeleteJobOfferAsync(id)) return false;
            OfferChanged(id);
            applicationCvs.ForEach(path => _fileStorage.Delete(path, FileCategory.Cv));
            return true;
        }

        private async Task<PagedViewModel<JobOfferViewModel>> SearchAsync(OfferQueryRequest query, bool onlyOpen, bool includeTestAccounts)
        {
            var result = await _jobOfferService.GetJobOffersAsync(ToFilter(query, onlyOpen, includeTestAccounts));
            return _mapper.Map<PagedViewModel<JobOfferViewModel>>(result);
        }

        private static OfferFilter ToFilter(OfferQueryRequest query, bool onlyOpen, bool includeTestAccounts) => new()
        {
            IncludeTestAccounts = includeTestAccounts,
            DomainId = query.DomainId,
            ProfessionId = query.ProfessionId,
            ContractType = query.ContractType,
            Search = query.Search,
            City = query.City,
            RecruiterProfileId = query.RecruiterProfileId,
            Page = query.Page,
            PageSize = query.PageSize,
            OnlyOpen = onlyOpen,
        };

        private static CandidateSummary ToSummary(CandidateProfile c) => new()
        {
            Id = c.Id,
            Headline = c.Headline,
            City = c.City,
            Situation = c.Situation,
            DesiredJob = c.DesiredJob,
            DesiredDomainId = c.DesiredDomainId,
            DesiredProfessionId = c.DesiredProfessionId,
            DesiredProfessionName = c.DesiredProfessionName,
            Skills = c.Skills,
            ExperienceTitles = c.Experiences.Select(e => e.Title).Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t!).ToList(),
            WishProfessionIds = c.WishProfessionIds,
        };

        private JobOfferViewModel WithOwner(JobOffer offer, ClaimsPrincipal user)
        {
            var viewModel = _mapper.Map<JobOfferViewModel>(offer);
            viewModel.IsOwner = offer.RecruiterUserId == user.GetUserId();
            return viewModel;
        }

        // Propriétaire de l'offre, ou membre de l'administration avec la permission
        private static void EnsureCanManage(ClaimsPrincipal user, JobOffer offer, string permission)
        {
            if (offer.RecruiterUserId != user.GetUserId() && !user.HasPermission(permission))
                throw new UnauthorizedAccessException();
        }

        // Entreprise désactivée par l'administration : son recruteur ne peut plus remettre ses offres en ligne
        private static void EnsureCompanyActive(ClaimsPrincipal user, JobOffer offer, string action)
        {
            if (offer.CompanyDisabled && !user.HasPermission(Permissions.Companies.Disable))
                throw new InvalidOperationException($"Votre entreprise a été désactivée : vous ne pouvez pas {action}. Contactez le support si vous pensez qu'il s'agit d'une erreur.");
        }

        private async Task ValidateAsync(JobOfferRequest model)
        {
            if (!ContractTypes.All.Contains(model.ContractType))
                throw new ArgumentException("Type de contrat invalide.");

            if ((model.RequiredSkills ?? []).Concat(model.BonusSkills ?? []).Any(s => s?.Trim().Length > 80))
                throw new ArgumentException("Une compétence ne peut pas dépasser 80 caractères.");

            if (await _jobDomainService.GetJobDomainByIdAsync(model.DomainId!.Value) == null)
                throw new ArgumentException("Le domaine sélectionné n'existe pas.");

            if (model.ProfessionId.HasValue)
            {
                var profession = await _professionService.GetProfessionByIdAsync(model.ProfessionId.Value);
                if (profession == null || profession.DomainId != model.DomainId)
                    throw new ArgumentException("Le métier ne correspond pas au domaine sélectionné.");
            }
        }
    }
}
