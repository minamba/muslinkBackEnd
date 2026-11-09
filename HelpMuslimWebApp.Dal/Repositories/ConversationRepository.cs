using HelpMuslimWebApp.Dal.Entities;
using HelpMuslimWebApp.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Models = HelpMuslimWebApp.Domain.Models;

namespace HelpMuslimWebApp.Dal.Repositories
{
    public class ConversationRepository : IConversationRepository
    {
        private readonly HelpMuslimWebAppDatabaseContext _context;

        public ConversationRepository(HelpMuslimWebAppDatabaseContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Models.Conversation>> GetConversationsAsync(string? userId)
        {
            return await Project(ForUser(userId), userId)
                .OrderByDescending(c => c.LastMessageAt ?? c.CreatedAt)
                .ToListAsync();
        }

        public async Task<Models.Conversation?> GetConversationByIdAsync(int id, string? userId = null)
        {
            return await Project(ForUser(userId).Where(c => c.Id == id), userId).FirstOrDefaultAsync();
        }

        public async Task<Models.Conversation> GetOrCreateConversationAsync(int recruiterProfileId, int candidateProfileId)
        {
            var entity = await _context.Conversations
                .FirstOrDefaultAsync(c => c.RecruiterProfileId == recruiterProfileId && c.CandidateProfileId == candidateProfileId);

            if (entity == null)
            {
                entity = new Conversation
                {
                    RecruiterProfileId = recruiterProfileId,
                    CandidateProfileId = candidateProfileId,
                    CreatedAt = DateTime.UtcNow,
                };
                _context.Conversations.Add(entity);
                await _context.SaveChangesAsync();
            }

            return (await GetConversationByIdAsync(entity.Id))!;
        }

        public Task<List<int>> GetContactedCandidateIdsAsync(int recruiterProfileId) =>
            _context.Conversations
                .Where(c => c.RecruiterProfileId == recruiterProfileId && c.CandidateProfileId != null
                            && c.Messages.Any(m => m.SenderUserId == c.RecruiterProfile!.UserId))
                .Select(c => c.CandidateProfileId!.Value)
                .ToListAsync();

        public async Task<IEnumerable<Models.Message>> GetMessagesAsync(int conversationId)
        {
            return await _context.Messages.AsNoTracking()
                .Where(m => m.ConversationId == conversationId)
                .OrderBy(m => m.SentAt)
                .Select(m => new Models.Message
                {
                    Id = m.Id, ConversationId = m.ConversationId, SenderUserId = m.SenderUserId, Content = m.Content,
                    JobApplicationId = m.JobApplicationId, SentAt = m.SentAt, ReadAt = m.ReadAt,
                    Kind = m.Kind, Data = m.Data, AttachmentPath = m.AttachmentPath, EditedAt = m.EditedAt, DeletedAt = m.DeletedAt,
                })
                .ToListAsync();
        }

        public async Task<Models.Message> AddMessageAsync(Models.Message model)
        {
            var conversation = await _context.Conversations.FirstAsync(c => c.Id == model.ConversationId);
            var entity = new Message
            {
                ConversationId = model.ConversationId,
                SenderUserId = model.SenderUserId,
                Content = model.Content,
                JobApplicationId = model.JobApplicationId,
                Kind = model.Kind,
                Data = model.Data,
                AttachmentPath = model.AttachmentPath,
                SentAt = model.SentAt ?? DateTime.UtcNow,
            };
            _context.Messages.Add(entity);
            conversation.LastMessageAt = entity.SentAt;
            await _context.SaveChangesAsync();
            model.Id = entity.Id;
            model.SentAt = entity.SentAt;
            return model;
        }

        public async Task<int> MarkConversationAsReadAsync(int conversationId, string readerUserId)
        {
            var now = DateTime.UtcNow;
            // Conversation lue : le prochain message pourra de nouveau déclencher un email d'alerte
            await _context.Conversations.Where(c => c.Id == conversationId && c.CandidateProfile!.UserId == readerUserId && c.CandidateAlertedAt != null)
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.CandidateAlertedAt, (DateTime?)null));
            await _context.Conversations.Where(c => c.Id == conversationId && c.RecruiterProfile!.UserId == readerUserId && c.RecruiterAlertedAt != null)
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.RecruiterAlertedAt, (DateTime?)null));
            return await _context.Messages
                .Where(m => m.ConversationId == conversationId && m.ReadAt == null && m.SenderUserId != readerUserId)
                .ExecuteUpdateAsync(s => s.SetProperty(m => m.ReadAt, now));
        }

        // En une seule requête : deux messages envoyés en même temps ne déclenchent pas deux emails
        public async Task<bool> TryMarkAlertedAsync(int conversationId, bool candidate)
        {
            var now = DateTime.UtcNow;
            return candidate
                ? await _context.Conversations.Where(c => c.Id == conversationId && c.CandidateAlertedAt == null)
                    .ExecuteUpdateAsync(s => s.SetProperty(c => c.CandidateAlertedAt, now)) > 0
                : await _context.Conversations.Where(c => c.Id == conversationId && c.RecruiterAlertedAt == null)
                    .ExecuteUpdateAsync(s => s.SetProperty(c => c.RecruiterAlertedAt, now)) > 0;
        }

        // CV partagés dans les conversations d'un candidat ou d'un recruteur (fichiers à effacer avec son profil)
        public async Task<List<string>> GetAttachmentPathsAsync(int? candidateProfileId, int? recruiterProfileId) =>
            await _context.Messages
                .Where(m => m.AttachmentPath != null
                    && ((candidateProfileId != null && m.Conversation!.CandidateProfileId == candidateProfileId)
                        || (recruiterProfileId != null && m.Conversation!.RecruiterProfileId == recruiterProfileId)))
                .Select(m => m.AttachmentPath!)
                .ToListAsync();

        public async Task<string?> GetAttachmentPathAsync(int messageId) =>
            await _context.Messages.Where(m => m.Id == messageId).Select(m => m.AttachmentPath).FirstOrDefaultAsync();

        public Task<int> GetUnreadCountAsync(string userId)
        {
            return _context.Messages.CountAsync(m =>
                m.ReadAt == null && m.DeletedAt == null && m.SenderUserId != userId &&
                (m.Conversation!.RecruiterProfile!.UserId == userId || m.Conversation.CandidateProfile!.UserId == userId) &&
                m.Conversation.CandidateProfile!.IsTestAccount == m.Conversation.RecruiterProfile!.IsTestAccount);
        }

        public async Task<bool> DeleteMessageAsync(int messageId)
        {
            var entity = await _context.Messages.FirstOrDefaultAsync(m => m.Id == messageId);
            if (entity == null) return false;

            _context.Messages.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<Models.Message?> GetMessageAsync(int messageId)
        {
            var entity = await _context.Messages.AsNoTracking().FirstOrDefaultAsync(m => m.Id == messageId);
            return entity == null ? null : ToModel(entity);
        }

        public async Task<Models.Message?> UpdateMessageContentAsync(int messageId, string content)
        {
            var entity = await _context.Messages.FirstOrDefaultAsync(m => m.Id == messageId);
            if (entity == null) return null;
            entity.Content = content;
            entity.EditedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return ToModel(entity);
        }

        public async Task<Models.Message?> MarkMessageDeletedAsync(int messageId)
        {
            var entity = await _context.Messages.FirstOrDefaultAsync(m => m.Id == messageId);
            if (entity == null) return null;
            entity.Content = string.Empty;
            entity.Kind = null;
            entity.Data = null;
            entity.AttachmentPath = null;
            entity.DeletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return ToModel(entity);
        }

        private static Models.Message ToModel(Message m) => new()
        {
            Id = m.Id, ConversationId = m.ConversationId, SenderUserId = m.SenderUserId, Content = m.Content,
            JobApplicationId = m.JobApplicationId, SentAt = m.SentAt, ReadAt = m.ReadAt,
            Kind = m.Kind, Data = m.Data, AttachmentPath = m.AttachmentPath, EditedAt = m.EditedAt, DeletedAt = m.DeletedAt,
        };

        private IQueryable<Conversation> ForUser(string? userId)
        {
            var query = _context.Conversations.AsNoTracking();
            // Compte de test face à un vrai compte (échange d'avant le marquage) : la conversation n'apparaît chez aucun des deux
            return userId == null
                ? query
                : query.Where(c => (c.RecruiterProfile!.UserId == userId || c.CandidateProfile!.UserId == userId)
                                   && c.CandidateProfile!.IsTestAccount == c.RecruiterProfile!.IsTestAccount);
        }

        private static IQueryable<Models.Conversation> Project(IQueryable<Conversation> query, string? userId) =>
            query.Select(c => new Models.Conversation
            {
                Id = c.Id,
                RecruiterProfileId = c.RecruiterProfileId,
                CandidateProfileId = c.CandidateProfileId,
                RecruiterUserId = c.RecruiterProfile!.UserId,
                CandidateUserId = c.CandidateProfile!.UserId,
                RecruiterName = c.RecruiterProfile.FirstName + " " + c.RecruiterProfile.LastName,
                RecruiterEmail = c.RecruiterProfile.Email,
                CompanyName = c.RecruiterProfile.CompanyName,
                CompanyLogoPath = c.RecruiterProfile.LogoPath,
                CandidateName = c.CandidateProfile.FirstName + " " + c.CandidateProfile.LastName,
                CandidateEmail = c.CandidateProfile.Email,
                CandidatePhotoPath = c.CandidateProfile.PhotoPath,
                CreatedAt = c.CreatedAt,
                LastMessageAt = c.LastMessageAt,
                LastMessage = c.Messages.OrderByDescending(m => m.SentAt).Select(m => m.DeletedAt != null ? "Message supprimé" : m.Content).FirstOrDefault(),
                UnreadCount = userId == null ? 0 : c.Messages.Count(m => m.ReadAt == null && m.DeletedAt == null && m.SenderUserId != userId),
            });
    }
}
