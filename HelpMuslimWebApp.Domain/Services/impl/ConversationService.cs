using HelpMuslimWebApp.Domain.Models;
using HelpMuslimWebApp.Domain.Repositories;

namespace HelpMuslimWebApp.Domain.Services.impl
{
    public class ConversationService : IConversationService
    {
        private readonly IConversationRepository _repository;

        public ConversationService(IConversationRepository repository)
        {
            _repository = repository;
        }

        public Task<IEnumerable<Conversation>> GetConversationsAsync(string? userId) => _repository.GetConversationsAsync(userId);
        public Task<Conversation?> GetConversationByIdAsync(int id, string? userId = null) => _repository.GetConversationByIdAsync(id, userId);
        public Task<Conversation> GetOrCreateConversationAsync(int recruiterProfileId, int candidateProfileId) => _repository.GetOrCreateConversationAsync(recruiterProfileId, candidateProfileId);
        public Task<IEnumerable<Message>> GetMessagesAsync(int conversationId) => _repository.GetMessagesAsync(conversationId);

        public Task<Message> AddMessageAsync(Message model)
        {
            model.Content = model.Content?.Trim();
            model.SentAt = DateTime.UtcNow;
            model.ReadAt = null;
            return _repository.AddMessageAsync(model);
        }

        public Task<int> MarkConversationAsReadAsync(int conversationId, string readerUserId) => _repository.MarkConversationAsReadAsync(conversationId, readerUserId);
        public Task<int> GetUnreadCountAsync(string userId) => _repository.GetUnreadCountAsync(userId);
        public Task<List<string>> GetAttachmentPathsAsync(int? candidateProfileId, int? recruiterProfileId) => _repository.GetAttachmentPathsAsync(candidateProfileId, recruiterProfileId);
        public Task<string?> GetAttachmentPathAsync(int messageId) => _repository.GetAttachmentPathAsync(messageId);
        public Task<bool> TryMarkAlertedAsync(int conversationId, bool candidate) => _repository.TryMarkAlertedAsync(conversationId, candidate);
        public Task<bool> DeleteMessageAsync(int messageId) => _repository.DeleteMessageAsync(messageId);
        public Task<List<int>> GetContactedCandidateIdsAsync(int recruiterProfileId) => _repository.GetContactedCandidateIdsAsync(recruiterProfileId);

        public async Task<Message?> EditMessageAsync(int conversationId, int messageId, string editorUserId, string content)
        {
            var message = await _repository.GetMessageAsync(messageId);
            if (message == null || message.ConversationId != conversationId) return null;
            if (message.SenderUserId != editorUserId) throw new UnauthorizedAccessException("Vous ne pouvez modifier que vos propres messages.");
            if (!message.IsEditable) throw new InvalidOperationException("Ce message ne peut pas être modifié.");

            content = content.Trim();
            if (content.Length == 0) throw new ArgumentException("Le message est vide.");
            // Texte inchangé : rien n'est noté
            if (content == message.Content) return message;
            return await _repository.UpdateMessageContentAsync(messageId, content);
        }

        public async Task<Message?> DeleteOwnMessageAsync(int conversationId, int messageId, string userId)
        {
            var message = await _repository.GetMessageAsync(messageId);
            if (message == null || message.ConversationId != conversationId) return null;
            if (message.SenderUserId != userId) throw new UnauthorizedAccessException("Vous ne pouvez supprimer que vos propres messages.");
            // Déjà supprimé : rien à refaire
            if (message.DeletedAt != null) return message;
            if (!message.IsDeletable) throw new InvalidOperationException("La lettre d'une candidature ne peut pas être supprimée de la conversation.");
            return await _repository.MarkMessageDeletedAsync(messageId);
        }
    }
}
