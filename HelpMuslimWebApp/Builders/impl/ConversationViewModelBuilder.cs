using System.Security.Claims;
using System.Text.Json;
using AutoMapper;
using HelpMuslimWebApp.Api.Request;
using HelpMuslimWebApp.Api.Services;
using HelpMuslimWebApp.Api.Utils;
using HelpMuslimWebApp.Api.ViewModels;
using HelpMuslimWebApp.Domain.Models;
using HelpMuslimWebApp.Domain.Services;

namespace HelpMuslimWebApp.Api.Builders.impl
{
    public class ConversationViewModelBuilder : IConversationViewModelBuilder
    {
        private readonly IMapper _mapper;
        private readonly IPremiumService _premiumService;
        private readonly TestModeGuard _testMode;
        private readonly IConversationService _conversationService;
        private readonly ICandidateProfileService _candidateService;
        private readonly IJobOfferService _jobOfferService;
        private readonly IProfileResolver _profileResolver;
        private readonly IRealtimeNotifier _notifier;
        private readonly IEmailService _emailService;
        private readonly ConversationPresence _presence;
        private readonly IRecruiterProfileService _recruiterService;
        private readonly IMatchAlertService _alerts;
        private readonly IFileStorageService _fileStorage;
        private readonly ILogger<ConversationViewModelBuilder> _logger;

        public ConversationViewModelBuilder(
            IMapper mapper,
            IPremiumService premiumService,
            TestModeGuard testMode,
            IConversationService conversationService,
            ICandidateProfileService candidateService,
            IJobOfferService jobOfferService,
            IProfileResolver profileResolver,
            IRealtimeNotifier notifier,
            IEmailService emailService,
            ConversationPresence presence,
            IRecruiterProfileService recruiterService,
            IMatchAlertService alerts,
            IFileStorageService fileStorage,
            ILogger<ConversationViewModelBuilder> logger)
        {
            _mapper = mapper;
            _premiumService = premiumService;
            _testMode = testMode;
            _conversationService = conversationService;
            _candidateService = candidateService;
            _jobOfferService = jobOfferService;
            _profileResolver = profileResolver;
            _notifier = notifier;
            _emailService = emailService;
            _presence = presence;
            _recruiterService = recruiterService;
            _alerts = alerts;
            _fileStorage = fileStorage;
            _logger = logger;
        }

        public async Task<IEnumerable<ConversationViewModel>> GetMyConversationsAsync(ClaimsPrincipal user)
        {
            var userId = user.GetUserId()!;
            var conversations = await _conversationService.GetConversationsAsync(userId);
            return conversations.Select(c => ToViewModel(c, userId));
        }

        public async Task<ConversationViewModel?> GetConversationAsync(ClaimsPrincipal user, int id)
        {
            var userId = user.GetUserId()!;
            var conversation = await _conversationService.GetConversationByIdAsync(id, userId);
            return conversation == null ? null : ToViewModel(conversation, userId);
        }

        public async Task<IEnumerable<MessageViewModel>?> GetMessagesAsync(ClaimsPrincipal user, int conversationId)
        {
            var userId = user.GetUserId()!;
            if (await _conversationService.GetConversationByIdAsync(conversationId, userId) == null) return null;

            // Ouvrir la conversation = lire les messages reçus
            if (await _conversationService.MarkConversationAsReadAsync(conversationId, userId) > 0)
                await NotifyUnreadAsync(userId);

            var messages = await _conversationService.GetMessagesAsync(conversationId);
            return messages.Select(m => ToMessageViewModel(m, userId));
        }

        public async Task<MessageViewModel?> SendMessageAsync(ClaimsPrincipal user, int conversationId, MessageRequest model)
        {
            var userId = user.GetUserId()!;
            var conversation = await _conversationService.GetConversationByIdAsync(conversationId, userId);
            if (conversation == null) return null;

            return await PostAsync(conversation, userId, new Message { Content = model.Content });
        }

        // Nouveau texte poussé aux deux participants (l'interlocuteur et les autres onglets de l'auteur) ; pas d'email
        public async Task<MessageViewModel?> EditMessageAsync(ClaimsPrincipal user, int conversationId, int messageId, MessageRequest model)
        {
            var userId = user.GetUserId()!;
            var conversation = await _conversationService.GetConversationByIdAsync(conversationId, userId);
            if (conversation == null) return null;

            var message = await _conversationService.EditMessageAsync(conversationId, messageId, userId, model.Content!);
            if (message == null) return null;

            await NotifyMessageChangedAsync(conversation, userId, message, unreadChanged: false);
            return ToMessageViewModel(message, userId);
        }

        // Remplacé par « Message supprimé » chez les deux participants ; CV joint effacé du stockage ; pas d'email
        public async Task<MessageViewModel?> DeleteOwnMessageAsync(ClaimsPrincipal user, int conversationId, int messageId)
        {
            var userId = user.GetUserId()!;
            var conversation = await _conversationService.GetConversationByIdAsync(conversationId, userId);
            if (conversation == null) return null;

            var attachment = await _conversationService.GetAttachmentPathAsync(messageId);
            var message = await _conversationService.DeleteOwnMessageAsync(conversationId, messageId, userId);
            if (message == null) return null;
            if (attachment != null && message.DeletedAt != null) _fileStorage.Delete(attachment, FileCategory.Cv);

            // Message non lu supprimé : le compteur de l'interlocuteur baisse
            await NotifyMessageChangedAsync(conversation, userId, message, unreadChanged: true);
            return ToMessageViewModel(message, userId);
        }

        // Nouvel état du message poussé à l'interlocuteur et aux autres onglets de l'auteur
        private async Task NotifyMessageChangedAsync(Conversation conversation, string userId, Message message, bool unreadChanged)
        {
            var recipientUserId = conversation.RecruiterUserId == userId ? conversation.CandidateUserId : conversation.RecruiterUserId;
            try
            {
                if (recipientUserId != null)
                {
                    await _notifier.MessageUpdatedAsync(recipientUserId, ToMessageViewModel(message, recipientUserId));
                    if (unreadChanged) await NotifyUnreadAsync(recipientUserId);
                }
                await _notifier.MessageUpdatedAsync(userId, ToMessageViewModel(message, userId));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Notification temps réel du message {MessageId} impossible.", message.Id);
            }
        }

        // ---------- Partages : numéro, fiche de poste, CV ----------

        private static readonly JsonSerializerOptions ShareJson = new(JsonSerializerDefaults.Web) { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull };

        // Numéro de téléphone du profil (recruteur ou candidat), envoyé tel qu'il est aujourd'hui
        public async Task<MessageViewModel?> SharePhoneAsync(ClaimsPrincipal user, int conversationId)
        {
            var userId = user.GetUserId()!;
            var conversation = await _conversationService.GetConversationByIdAsync(conversationId, userId);
            if (conversation == null) return null;

            var phone = conversation.RecruiterUserId == userId
                ? (await _recruiterService.GetRecruiterProfileByIdAsync(conversation.RecruiterProfileId!.Value))?.Phone
                : (await _candidateService.GetCandidateProfileByIdAsync(conversation.CandidateProfileId!.Value))?.Phone;
            if (string.IsNullOrWhiteSpace(phone))
                throw new ArgumentException("Aucun numéro de téléphone dans votre profil : ajoutez-le pour pouvoir l'envoyer.");

            phone = phone.Trim();
            return await PostAsync(conversation, userId, new Message
            {
                Kind = MessageKinds.Phone,
                Content = $"📱 Mon numéro de téléphone : {phone}",
                Data = JsonSerializer.Serialize(new MessageShareViewModel { Phone = phone }, ShareJson),
            });
        }

        // Recruteur : lien vers l'une de ses offres
        public async Task<MessageViewModel?> ShareOfferAsync(ClaimsPrincipal user, int conversationId, int offerId)
        {
            var userId = user.GetUserId()!;
            var conversation = await _conversationService.GetConversationByIdAsync(conversationId, userId);
            if (conversation == null) return null;
            if (conversation.RecruiterUserId != userId) throw new InvalidOperationException("Seul le recruteur peut envoyer une fiche de poste.");

            var offer = await _jobOfferService.GetJobOfferByIdAsync(offerId);
            if (offer == null || offer.RecruiterProfileId != conversation.RecruiterProfileId)
                throw new ArgumentException("Cette offre n'existe pas ou ne vous appartient pas.");

            return await PostAsync(conversation, userId, new Message
            {
                Kind = MessageKinds.Offer,
                Content = $"📄 Fiche de poste : {offer.Title}",
                Data = JsonSerializer.Serialize(new MessageShareViewModel
                {
                    OfferId = offer.Id, OfferTitle = offer.Title, CompanyName = offer.CompanyName, City = offer.City, ContractType = offer.ContractType,
                }, ShareJson),
            });
        }

        // Candidat : copie de son CV au moment de l'envoi (un CV remplacé ensuite ne change pas ce qui a été envoyé)
        public async Task<MessageViewModel?> ShareCvAsync(ClaimsPrincipal user, int conversationId)
        {
            var userId = user.GetUserId()!;
            var conversation = await _conversationService.GetConversationByIdAsync(conversationId, userId);
            if (conversation == null) return null;
            if (conversation.CandidateUserId != userId) throw new InvalidOperationException("Seul le candidat peut envoyer son CV.");

            var candidate = await _candidateService.GetCandidateProfileByIdAsync(conversation.CandidateProfileId!.Value);
            var copy = candidate?.CvPath == null ? null : _fileStorage.Copy(candidate.CvPath, FileCategory.Cv);
            if (copy == null)
                throw new ArgumentException("Aucun CV dans votre profil : ajoutez-le dans « Mon profil » pour pouvoir l'envoyer.");

            var fileName = candidate!.CvFileName ?? Path.GetFileName(candidate.CvPath!);
            return await PostAsync(conversation, userId, new Message
            {
                Kind = MessageKinds.Cv,
                Content = $"📎 Mon CV : {fileName}",
                Data = JsonSerializer.Serialize(new MessageShareViewModel { FileName = fileName }, ShareJson),
                AttachmentPath = copy,
            });
        }

        // CV envoyé dans une conversation : lisible par les deux participants seulement
        public async Task<(Stream Stream, string FileName, string ContentType)?> GetMessageCvAsync(ClaimsPrincipal user, int conversationId, int messageId)
        {
            if (await _conversationService.GetConversationByIdAsync(conversationId, user.GetUserId()!) == null) return null;
            var message = (await _conversationService.GetMessagesAsync(conversationId)).FirstOrDefault(m => m.Id == messageId);
            if (message?.Kind != MessageKinds.Cv || message.AttachmentPath == null) return null;

            var stream = _fileStorage.OpenRead(message.AttachmentPath, FileCategory.Cv);
            if (stream == null) return null;
            var fileName = ReadShare(message)?.FileName ?? Path.GetFileName(message.AttachmentPath);
            return (stream, fileName, _fileStorage.GetContentType(fileName));
        }

        // Ajout d'un message (écrit ou partage), puis notification temps réel et email d'alerte au destinataire
        private async Task<MessageViewModel> PostAsync(Conversation conversation, string userId, Message draft)
        {
            draft.ConversationId = conversation.Id;
            draft.SenderUserId = userId;
            var message = await _conversationService.AddMessageAsync(draft);

            var senderIsRecruiter = conversation.RecruiterUserId == userId;
            var recipientUserId = senderIsRecruiter ? conversation.CandidateUserId : conversation.RecruiterUserId;

            // Le recruteur écrit au candidat : ses offres auxquelles ce candidat a postulé passent "En discussion"
            if (senderIsRecruiter && conversation.RecruiterProfileId.HasValue && conversation.CandidateProfileId.HasValue)
                await _jobOfferService.MarkOffersInDiscussionAsync(conversation.RecruiterProfileId.Value, conversation.CandidateProfileId.Value);

            if (recipientUserId != null)
            {
                try
                {
                    // L'expéditeur est l'interlocuteur du destinataire
                    var sender = ToViewModel(conversation, recipientUserId);
                    var notification = ToMessageViewModel(message, recipientUserId);
                    notification.SenderName = sender.OtherPartyName;
                    notification.SenderSubtitle = sender.OtherPartySubtitle;
                    notification.SenderAvatarUrl = sender.OtherPartyAvatarUrl;
                    await _notifier.MessageReceivedAsync(recipientUserId, notification);
                    await NotifyUnreadAsync(recipientUserId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Notification temps réel du message {MessageId} impossible.", message.Id);
                }
            }

            try
            {
                await AlertRecipientAsync(conversation, senderIsRecruiter, recipientUserId, message.Content ?? string.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Envoi de l'email du message {MessageId} impossible.", message.Id);
            }

            return ToMessageViewModel(message, userId);
        }

        // Email « nouveau message » au destinataire : pas s'il lit déjà la discussion à l'écran (échange en direct),
        // et un seul par conversation tant qu'il ne l'a pas ouverte (plusieurs messages d'affilée : un email).
        // Recruteur : seulement s'il n'a pas coupé ces emails.
        private async Task AlertRecipientAsync(Conversation conversation, bool senderIsRecruiter, string? recipientUserId, string content)
        {
            if (recipientUserId == null || _presence.IsViewing(recipientUserId, conversation.Id)) return;

            if (senderIsRecruiter)
            {
                if (string.IsNullOrWhiteSpace(conversation.CandidateEmail)) return;
                if (!await _conversationService.TryMarkAlertedAsync(conversation.Id, candidate: true)) return;
                await _emailService.SendNewMessageEmailAsync(
                    conversation.CandidateEmail, conversation.CandidateName?.Trim() ?? string.Empty,
                    conversation.RecruiterName?.Trim() ?? "Un recruteur", conversation.CompanyName, content, conversation.Id);
                return;
            }

            if (conversation.RecruiterProfileId is not { } recruiterProfileId) return;
            var recruiter = await _recruiterService.GetRecruiterProfileByIdAsync(recruiterProfileId);
            if (recruiter == null || recruiter.IsDisabled || !recruiter.MessageAlertsEnabled || string.IsNullOrWhiteSpace(recruiter.Email)) return;
            if (!await _conversationService.TryMarkAlertedAsync(conversation.Id, candidate: false)) return;
            await _emailService.SendNewCandidateMessageEmailAsync(
                recruiter.Email, recruiter.FirstName?.Trim() ?? string.Empty, conversation.CandidateName?.Trim() ?? "Un candidat",
                content, conversation.Id, await _alerts.GetOrCreateTokenAsync(recruiterProfileId));
        }

        public async Task<ConversationViewModel?> StartConversationAsync(ClaimsPrincipal user, StartConversationRequest model)
        {
            var recruiter = await _profileResolver.GetOrCreateRecruiterAsync(user);
            // Compte de test : un candidat de test n'échange qu'avec un recruteur de test, et inversement
            if (await _candidateService.GetCandidateProfileByIdAsync(model.CandidateProfileId!.Value) is not { } candidate
                || (candidate.IsTestAccount && !user.SeesTestCandidates())) return null;
            if (candidate.IsTestAccount != recruiter.IsTestAccount)
                throw new InvalidOperationException("Compte de test : vous ne pouvez écrire qu'aux candidats de test.");
            var exempt = await _testMode.ExemptsAsync(user);
            if (!exempt) CompanyVerification.Ensure(recruiter, CompanyVerification.CandidatesMessage);

            // Recruteur Pro activé : un recruteur gratuit écrit aux candidats qui ont postulé à ses offres
            // (ou avec qui il échange déjà) ; aller chercher les autres est réservé au Pro
            if (!exempt && !Premium.IsActive(recruiter.ProUntil) && (await _premiumService.GetSettingsAsync()).RecruitersEnabled
                && !await _candidateService.HasRelationWithRecruiterAsync(model.CandidateProfileId.Value, recruiter.Id))
                throw new InvalidOperationException("Contacter un candidat qui n'a pas postulé à vos offres est réservé à Recruteur Pro.");

            var conversation = await _conversationService.GetOrCreateConversationAsync(recruiter.Id, model.CandidateProfileId.Value);
            return ToViewModel(conversation, user.GetUserId()!);
        }

        public Task<int> GetUnreadCountAsync(ClaimsPrincipal user) => _conversationService.GetUnreadCountAsync(user.GetUserId()!);

        public async Task<IEnumerable<ConversationViewModel>> GetAllConversationsAsync()
        {
            var conversations = await _conversationService.GetConversationsAsync(null);
            return conversations.Select(c => ToViewModel(c, null));
        }

        public async Task<IEnumerable<MessageViewModel>?> GetConversationMessagesAsync(int conversationId)
        {
            if (await _conversationService.GetConversationByIdAsync(conversationId) == null) return null;
            var messages = await _conversationService.GetMessagesAsync(conversationId);
            return messages.Select(m => ToMessageViewModel(m, null));
        }

        // Administration : message supprimé avec le CV qui y était joint
        public async Task<bool> DeleteMessageAsync(int messageId)
        {
            var attachment = await _conversationService.GetAttachmentPathAsync(messageId);
            if (!await _conversationService.DeleteMessageAsync(messageId)) return false;
            _fileStorage.Delete(attachment, FileCategory.Cv);
            return true;
        }

        private async Task NotifyUnreadAsync(string userId) =>
            await _notifier.UnreadCountChangedAsync(userId, await _conversationService.GetUnreadCountAsync(userId));

        private MessageViewModel ToMessageViewModel(Message message, string? viewerUserId)
        {
            var viewModel = _mapper.Map<MessageViewModel>(message);
            viewModel.IsMine = viewerUserId != null && message.SenderUserId == viewerUserId;
            viewModel.Share = ReadShare(message);
            return viewModel;
        }

        private static MessageShareViewModel? ReadShare(Message message)
        {
            if (string.IsNullOrWhiteSpace(message.Data)) return null;
            try { return JsonSerializer.Deserialize<MessageShareViewModel>(message.Data, ShareJson); }
            catch (JsonException) { return null; }
        }

        // Interlocuteur vu depuis l'utilisateur connecté (viewer = null : vue admin)
        private static ConversationViewModel ToViewModel(Conversation c, string? viewerUserId)
        {
            var viewerIsRecruiter = viewerUserId != null && c.RecruiterUserId == viewerUserId;
            return new ConversationViewModel
            {
                Id = c.Id,
                RecruiterProfileId = c.RecruiterProfileId,
                CandidateProfileId = c.CandidateProfileId,
                OtherPartyName = (viewerIsRecruiter ? c.CandidateName : c.RecruiterName)?.Trim(),
                OtherPartySubtitle = viewerIsRecruiter ? "Candidat" : c.CompanyName,
                OtherPartyAvatarUrl = viewerIsRecruiter ? c.CandidatePhotoPath : c.CompanyLogoPath,
                LastMessage = c.LastMessage,
                LastMessageAt = c.LastMessageAt,
                UnreadCount = c.UnreadCount,
                CreatedAt = c.CreatedAt,
                RecruiterUserId = viewerUserId == null ? c.RecruiterUserId : null,
                RecruiterName = c.RecruiterName?.Trim(),
                CompanyName = c.CompanyName,
                CandidateName = c.CandidateName?.Trim(),
            };
        }
    }
}
