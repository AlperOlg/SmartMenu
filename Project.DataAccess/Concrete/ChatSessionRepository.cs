using Microsoft.EntityFrameworkCore;
using Project.Core.Entities;
using Project.DataAccess.Abstract;

namespace Project.DataAccess.Concrete;

public class ChatSessionRepository : GenericRepository<ChatSession>, IChatSessionRepository
{
    public ChatSessionRepository(SmartMenuDbContext context) : base(context)
    {
    }

    public async Task<ChatSession?> GetSessionWithMessagesAsync(int sessionId, int userId)
    {
        return await _context.ChatSessions
            .Include(cs => cs.Messages.OrderBy(m => m.SentAt).ThenBy(m => m.Id))
            .AsNoTracking()
            .FirstOrDefaultAsync(cs => cs.Id == sessionId && cs.AppUserId == userId);
    }

    public async Task<List<ChatSession>> GetUserSessionsWithMessagesAsync(int userId)
    {
        return await _context.ChatSessions
            .Where(cs => cs.AppUserId == userId)
            .Include(cs => cs.Messages.OrderBy(m => m.SentAt).ThenBy(m => m.Id))
            .OrderByDescending(cs => cs.CreatedAt)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<int> GetActiveSessionCountAsync(int userId)
    {
        return await _context.ChatSessions
            .CountAsync(cs => cs.AppUserId == userId && cs.IsActive);
    }

    public async Task<int> GetMessageCountInSessionAsync(int sessionId)
    {
        return await _context.ChatMessages
            .CountAsync(m => m.ChatSessionId == sessionId);
    }

    public async Task AddMessageAsync(ChatMessage message)
    {
        await _context.ChatMessages.AddAsync(message);
        await _context.SaveChangesAsync();
    }
}
