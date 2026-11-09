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
    public class JobApplicationViewModelBuilder : IJobApplicationViewModelBuilder
    {
        private readonly IMapper _mapper;
        private readonly IPremiumService _premiumService;
        private readonly IJobApplicationService _applicationService;
        private readonly IJobOfferService _jobOfferService;
        private readonly ICandidateProfileService _candidateService;
        private readonly IRecruiterProfileService _recruiterService;
        private readonly IConversationService _conversationService;
        private readonly IFavoriteService _favoriteService;
        private readonly IProfileResolver _profileResolver;
        private readonly IRealtimeNotifier _notifier;
        private readonly IEmailService _emailService;
        private readonly IFileStorageService _fileStorage;
        private readonly IMatchAlertService _alerts;
        private readonly ILogger<JobApplicationViewModelBuilder> _logger;

        public JobApplicationViewModelBuilder(
            IMapper mapper,
            IPremiumService premiumService,
            IJobApplicationService applicationService,
            IJobOfferService jobOfferService,
            ICandidateProfileService candidateService,
            IRecruiterProfileService recruiterService,
            IConversationService conversationService,
            IFavoriteService favoriteService,
            IProfileResolver profileResolver,
            IRealtimeNotifier notifier,
            IEmailService emailService,
            IFileStorageService fileStorage,
            IMatchAlertService alerts,
            ILogger<JobApplicationViewModelBuilder> logger)
        {
            _alerts = alerts;
            _mapper = mapper;
            _premiumService = premiumService;
            _applicationService = applicationService;
            _jobOfferService = jobOfferService;
            _candidateService = candidateService;
            _recruiterService = recruiterService;
            _conversationService = conversationService;
            _favoriteService = favoriteService;
            _profileResolver = profileResolver;
            _notifier = notifier;
            _emailService = emailService;
            _fileStorage = fileStorage;
            _logger = logger;
        }

        public async Task<JobApplicationViewModel?> ApplyAsync(ClaimsPrincipal user, JobApplicationRequest model)
        {
            var candidate = await _profileResolver.GetOrCreateCandidateAsync(user);
            var offer = await _jobOfferService.GetJobOfferByIdAsync(model.JobOfferId!.Value)
                ?? throw new KeyNotFoundException("Offre introuvable.");

            if (offer.IsOpen != true)
                throw new InvalidOperationException("Cette offre est fermée, il n'est plus possible d'y postuler.");
            if (offer.IsPubliclyHidden)
                throw new InvalidOperationException("Cette offre n'est plus disponible.");
            // Compte de test : jamais de candidature chez un vrai recruteur (ni message, ni email, ni profil chez lui)
            var recruiter = await _recruiterService.GetRecruiterProfileByIdAsync(offer.RecruiterProfileId!.Value);
            if (candidate.IsTestAccount && recruiter?.IsTestAccount != true)
                throw new InvalidOperationException("Compte de test : vous ne pouvez postuler qu'aux offres des recruteurs de test.");
            if (!candidate.IsTestAccount && recruiter?.IsTestAccount == true)
                throw new KeyNotFoundException("Offre introuvable.");

            // 1. Doublon : une seule candidature par offre
            if (await _applicationService.JobApplicationExistsAsync(offer.Id, candidate.Id))
                return null;

            // 2. Enregistrement de la candidature, avec une copie du CV du profil si le candidat la joint
            var cvPath = model.AttachCv ? _fileStorage.Copy(candidate.CvPath, FileCategory.Cv) : null;
            var application = await _applicationService.AddJobApplicationAsync(new JobApplication
            {
                JobOfferId = offer.Id,
                CandidateProfileId = candidate.Id,
                CoverLetter = model.CoverLetter,
                CvPath = cvPath,
                CvFileName = cvPath == null ? null : candidate.CvFileName,
            });

            // 3. Message dans la messagerie du recruteur, avec la lettre de motivation
            var conversation = await _conversationService.GetOrCreateConversationAsync(offer.RecruiterProfileId!.Value, candidate.Id);
            var message = await _conversationService.AddMessageAsync(new Message
            {
                ConversationId = conversation.Id,
                SenderUserId = candidate.UserId,
                JobApplicationId = application.Id,
                Content = $"Nouvelle candidature pour l'offre « {offer.Title} »\n\n{model.CoverLetter!.Trim()}",
            });

            // 4. Notifications : temps réel + email (une erreur ici ne doit pas bloquer la candidature)
            var candidateName = $"{candidate.FirstName} {candidate.LastName}".Trim();
            try
            {
                if (conversation.RecruiterUserId != null)
                {
                    var viewModel = _mapper.Map<MessageViewModel>(message);
                    viewModel.SenderName = candidateName;
                    viewModel.SenderSubtitle = $"Candidature · {offer.Title}";
                    viewModel.SenderAvatarUrl = candidate.PhotoPath;
                    await _notifier.MessageReceivedAsync(conversation.RecruiterUserId, viewModel);
                    await _notifier.UnreadCountChangedAsync(conversation.RecruiterUserId,
                        await _conversationService.GetUnreadCountAsync(conversation.RecruiterUserId));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Notification temps réel de la candidature {ApplicationId} impossible.", application.Id);
            }

            try
            {
                // Email coupé par le recruteur : la candidature reste dans son espace et sa messagerie
                if (recruiter is { ApplicationAlertsEnabled: true, IsDisabled: false } && !string.IsNullOrWhiteSpace(recruiter.Email))
                {
                    await _emailService.SendNewApplicationEmailAsync(
                        recruiter.Email, $"{recruiter.FirstName} {recruiter.LastName}".Trim(), candidateName, offer.Title!, application.Id,
                        await _alerts.GetOrCreateTokenAsync(recruiter.Id));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Envoi de l'email de candidature {ApplicationId} impossible.", application.Id);
            }

            return _mapper.Map<JobApplicationViewModel>(application);
        }

        public async Task<IEnumerable<JobApplicationViewModel>> GetMyApplicationsAsync(ClaimsPrincipal user)
        {
            var candidate = await _profileResolver.GetOrCreateCandidateAsync(user);
            return _mapper.Map<IEnumerable<JobApplicationViewModel>>(
                await _applicationService.GetCandidateApplicationsAsync(candidate.Id));
        }

        public async Task<IEnumerable<JobApplicationViewModel>?> GetApplicationsForOfferAsync(ClaimsPrincipal user, int jobOfferId)
        {
            var offer = await _jobOfferService.GetJobOfferByIdAsync(jobOfferId);
            if (offer == null) return null;

            if (offer.RecruiterUserId != user.GetUserId() && !user.HasPermission(Permissions.Applications.View))
                throw new UnauthorizedAccessException();

            var premiumEnabled = (await _premiumService.GetSettingsAsync()).CandidatesEnabled;
            var applications = await _applicationService.GetJobApplicationsAsync(jobOfferId: jobOfferId);
            var contacted = offer.RecruiterProfileId is { } recruiterId ? await _conversationService.GetContactedCandidateIdsAsync(recruiterId) : [];
            return Scored(user, applications.Select(a => (a, offer)), premiumEnabled, contacted);
        }

        // Toutes les candidatures reçues sur les offres du recruteur (ouvertes et fermées), chacune avec sa correspondance à son offre
        public async Task<IEnumerable<JobApplicationViewModel>> GetReceivedApplicationsAsync(ClaimsPrincipal user)
        {
            var recruiter = await _profileResolver.GetOrCreateRecruiterAsync(user);
            var offers = (await _jobOfferService.GetJobOffersAsync(new OfferFilter { RecruiterProfileId = recruiter.Id, OnlyOpen = false, PageSize = 1000 })).Items;
            var premiumEnabled = (await _premiumService.GetSettingsAsync()).CandidatesEnabled;

            var pairs = new List<(JobApplication, JobOffer)>();
            foreach (var offer in offers)
                pairs.AddRange((await _applicationService.GetJobApplicationsAsync(jobOfferId: offer.Id)).Select(a => (a, offer)));
            return Scored(user, pairs, premiumEnabled, await _conversationService.GetContactedCandidateIdsAsync(recruiter.Id));
        }

        private static List<SkillCheckViewModel> SkillChecks(IEnumerable<string> candidateSkills, JobOffer offer) =>
            CandidateMatcher.CheckSkills(candidateSkills, offer)
                .Select(s => new SkillCheckViewModel { Skill = s.Skill, Required = s.Required, CandidateSkill = s.CandidateSkill }).ToList();

        // Correspondance de chaque candidature avec son offre. Classées de la plus proche à la plus éloignée (puis la plus récente) ;
        // mode premium actif : bonus de classement pour les candidats premium (le score affiché reste le vrai, badge « Premium »).
        // contacted : candidats à qui le recruteur a déjà écrit (badge « Vous échangez »)
        private List<JobApplicationViewModel> Scored(ClaimsPrincipal user, IEnumerable<(JobApplication Application, JobOffer Offer)> items, bool premiumEnabled, IEnumerable<int> contacted)
        {
            var inDiscussion = contacted.ToHashSet();
            var applications = items
                // Candidats de test : jamais montrés à un vrai recruteur
                .Where(x => x.Application.Candidate?.IsTestAccount != true || user.SeesTestCandidates())
                .Select(x =>
                {
                    var score = x.Application.Candidate == null ? 0 : CandidateMatcher.RankOffers(x.Application.Candidate, [x.Offer])[0].Score;
                    var isPremium = premiumEnabled && x.Application.Candidate != null && Premium.IsActive(x.Application.Candidate.PremiumUntil);
                    return (x.Application, x.Offer, Score: score, IsPremium: isPremium);
                })
                .OrderByDescending(x => Premium.RankingKey(x.Score, x.IsPremium))
                .ThenByDescending(x => x.Score)
                .ThenByDescending(x => x.Application.CreatedAt)
                .Select(x =>
                {
                    var viewModel = _mapper.Map<JobApplicationViewModel>(x.Application);
                    viewModel.MatchScore = x.Score;
                    viewModel.Skills = x.Application.Candidate == null ? null : SkillChecks(x.Application.Candidate.Skills, x.Offer);
                    viewModel.InDiscussion = x.Application.CandidateProfileId is { } candidateId && inDiscussion.Contains(candidateId);
                    if (viewModel.Candidate != null) viewModel.Candidate.IsPremium = x.IsPremium;
                    return viewModel;
                })
                .ToList();
            // Recruteur : pas d'email dans la liste (il apparaît sur la fiche si le candidat l'a rendu visible)
            if (!user.HasPermission(Permissions.Applications.View)) applications.ForEach(a => CandidateContact.Strip(a.Candidate));
            return applications;
        }

        public async Task<IEnumerable<JobApplicationViewModel>> GetAllApplicationsAsync() =>
            _mapper.Map<IEnumerable<JobApplicationViewModel>>(await _applicationService.GetJobApplicationsAsync());

        public async Task<JobApplicationDetailViewModel?> GetApplicationByIdAsync(ClaimsPrincipal user, int id)
        {
            var application = await _applicationService.GetJobApplicationByIdAsync(id);
            if (application == null) return null;

            // Recruteur propriétaire de l'offre, candidat auteur, ou administration
            var recruiter = await _profileResolver.FindRecruiterAsync(user);
            var isOfferOwner = recruiter != null && recruiter.Id == application.RecruiterProfileId;
            var isAuthor = application.Candidate?.UserId == user.GetUserId();
            if (!isOfferOwner && !isAuthor && !user.HasPermission(Permissions.Applications.View))
                throw new UnauthorizedAccessException();

            var offer = await _jobOfferService.GetJobOfferByIdAsync(application.JobOfferId!.Value);
            var candidate = await _candidateService.GetCandidateProfileByIdAsync(application.CandidateProfileId!.Value);
            if (candidate?.IsTestAccount == true && !isAuthor && !user.SeesTestCandidates()) return null;

            var candidateViewModel = candidate == null ? null : _mapper.Map<CandidateProfileViewModel>(candidate);
            if (candidateViewModel != null && recruiter != null)
                candidateViewModel.IsFavorite = await _favoriteService.FavoriteExistsAsync(recruiter.Id, candidateViewModel.Id);

            // Recruteur : coordonnées seulement si le candidat a choisi de les montrer
            var asRecruiter = !isAuthor && !user.HasPermission(Permissions.Applications.View);
            if (asRecruiter && candidateViewModel != null) CandidateContact.ApplyVisibility(candidateViewModel);
            var applicationViewModel = _mapper.Map<JobApplicationViewModel>(application);
            if (asRecruiter) CandidateContact.Strip(applicationViewModel.Candidate);

            var offerViewModel = offer == null ? null : _mapper.Map<JobOfferViewModel>(offer);
            if (offerViewModel != null)
            {
                offerViewModel.IsOwner = isOfferOwner;
                offerViewModel.HasApplied = isAuthor;
            }

            // Recruteur de l'offre ou administration : ce que le candidat a, et ce qui lui manque, pour cette offre
            ApplicationMatchViewModel? match = null;
            if ((isOfferOwner || user.HasPermission(Permissions.Applications.View)) && offer != null && candidate != null)
            {
                applicationViewModel.InDiscussion = application.RecruiterProfileId is { } recruiterId
                    && (await _conversationService.GetContactedCandidateIdsAsync(recruiterId)).Contains(candidate.Id);
                match = new ApplicationMatchViewModel
                {
                    Score = application.Candidate == null ? 0 : CandidateMatcher.RankOffers(application.Candidate, [offer])[0].Score,
                    Skills = SkillChecks(candidate.Skills, offer),
                };
            }

            return new JobApplicationDetailViewModel
            {
                Application = applicationViewModel,
                Offer = offerViewModel,
                Candidate = candidateViewModel,
                Match = match,
            };
        }

        // Candidat : retirer de sa liste une candidature dont l'offre est fermée (le recruteur la conserve)
        public async Task<bool> HideMyApplicationAsync(ClaimsPrincipal user, int id)
        {
            var application = await _applicationService.GetJobApplicationByIdAsync(id);
            if (application == null) return false;
            if (application.Candidate?.UserId != user.GetUserId()) throw new UnauthorizedAccessException();
            if (application.OfferIsOpen == true)
                throw new InvalidOperationException("L'offre est toujours ouverte : retirez votre candidature si vous ne souhaitez plus y donner suite.");

            return await _applicationService.HideJobApplicationAsync(id);
        }

        public async Task<bool> DeleteApplicationAsync(ClaimsPrincipal user, int id)
        {
            var application = await _applicationService.GetJobApplicationByIdAsync(id);
            if (application == null) return false;

            // Le candidat peut retirer sa candidature ; l'administration peut la supprimer
            if (application.Candidate?.UserId != user.GetUserId() && !user.HasPermission(Permissions.Applications.Delete))
                throw new UnauthorizedAccessException();

            if (!await _applicationService.DeleteJobApplicationAsync(id)) return false;
            _fileStorage.Delete(application.CvPath, FileCategory.Cv);
            return true;
        }

        // CV joint à la candidature : recruteur de l'offre, candidat auteur ou administration
        public async Task<(Stream Stream, string FileName, string ContentType)?> GetApplicationCvAsync(ClaimsPrincipal user, int id)
        {
            var application = await _applicationService.GetJobApplicationByIdAsync(id);
            if (application?.CvPath == null) return null;

            var recruiter = await _profileResolver.FindRecruiterAsync(user);
            var isOfferOwner = recruiter != null && recruiter.Id == application.RecruiterProfileId;
            var isAuthor = application.Candidate?.UserId == user.GetUserId();
            if (!isOfferOwner && !isAuthor && !user.HasPermission(Permissions.Applications.View))
                throw new UnauthorizedAccessException();

            var stream = _fileStorage.OpenRead(application.CvPath, FileCategory.Cv);
            if (stream == null) return null;
            return (stream, application.CvFileName ?? Path.GetFileName(application.CvPath), _fileStorage.GetContentType(application.CvPath));
        }
    }
}
