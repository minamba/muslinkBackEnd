using HelpMuslimWebApp.Domain.Models;

namespace HelpMuslimWebApp.Domain.Services
{
    public interface IConversationService
    {
        Task<IEnumerable<Conversation>> GetConversationsAsync(string? userId);
        Task<Conversation?> GetConversationByIdAsync(int id, string? userId = null);
        Task<Conversation> GetOrCreateConversationAsync(int recruiterProfileId, int candidateProfileId);
        Task<IEnumerable<Message>> GetMessagesAsync(int conversationId);
        Task<Message> AddMessageAsync(Message model);
        Task<int> MarkConversationAsReadAsync(int conversationId, string readerUserId);
        Task<bool> TryMarkAlertedAsync(int conversationId, bool candidate);
        Task<int> GetUnreadCountAsync(string userId);
        // CV partagés dans la messagerie : fichiers à effacer avec un profil ou un message
        Task<List<string>> GetAttachmentPathsAsync(int? candidateProfileId, int? recruiterProfileId);
        Task<string?> GetAttachmentPathAsync(int messageId);
        Task<bool> DeleteMessageAsync(int messageId);
        // Son propre message écrit, dans cette conversation (null : introuvable). Message d'un autre : UnauthorizedAccessException ; partage ou candidature : InvalidOperationException
        Task<Message?> EditMessageAsync(int conversationId, int messageId, string editorUserId, string content);
        // Son propre message (null : introuvable). Message d'un autre : UnauthorizedAccessException ; candidature : InvalidOperationException
        Task<Message?> DeleteOwnMessageAsync(int conversationId, int messageId, string userId);
        // Candidats à qui le recruteur a écrit au moins un message (la candidature elle-même ne compte pas)
        Task<List<int>> GetContactedCandidateIdsAsync(int recruiterProfileId);
    }
}
