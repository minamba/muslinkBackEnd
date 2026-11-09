using HelpMuslimWebApp.Domain.Models;

namespace HelpMuslimWebApp.Domain.Repositories
{
    public interface IConversationRepository
    {
        // userId = null → toutes les conversations (admin)
        Task<IEnumerable<Conversation>> GetConversationsAsync(string? userId);
        Task<Conversation?> GetConversationByIdAsync(int id, string? userId = null);
        Task<Conversation> GetOrCreateConversationAsync(int recruiterProfileId, int candidateProfileId);
        Task<IEnumerable<Message>> GetMessagesAsync(int conversationId);
        Task<Message> AddMessageAsync(Message model);
        Task<int> MarkConversationAsReadAsync(int conversationId, string readerUserId);
        // Email d'alerte : true si aucun n'attend déjà d'être lu (et le note), false sinon
        Task<bool> TryMarkAlertedAsync(int conversationId, bool candidate);
        Task<int> GetUnreadCountAsync(string userId);
        Task<List<string>> GetAttachmentPathsAsync(int? candidateProfileId, int? recruiterProfileId);
        Task<string?> GetAttachmentPathAsync(int messageId);
        Task<bool> DeleteMessageAsync(int messageId);
        Task<Message?> GetMessageAsync(int messageId);
        // Nouveau texte, date de modification notée
        Task<Message?> UpdateMessageContentAsync(int messageId, string content);
        // Texte, partage et pièce jointe effacés, date de suppression notée
        Task<Message?> MarkMessageDeletedAsync(int messageId);
        // Candidats à qui le recruteur a écrit au moins un message (la candidature elle-même ne compte pas)
        Task<List<int>> GetContactedCandidateIdsAsync(int recruiterProfileId);
    }
}
