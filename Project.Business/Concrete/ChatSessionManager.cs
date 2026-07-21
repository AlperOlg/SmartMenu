using Project.Business.Abstract;
using Project.Core.Entities;
using Project.DataAccess.Abstract;

namespace Project.Business.Concrete;

public class ChatSessionManager : IChatSessionService
{
    private const int MaxSessionsPerUser = 5;
    private const int MaxMessagesPerSession = 5;

    private readonly IChatSessionRepository _chatSessionRepository;

    public ChatSessionManager(IChatSessionRepository chatSessionRepository)
    {
        _chatSessionRepository = chatSessionRepository;
    }

    public async Task<ChatSession> CreateSessionAsync(int userId)
    {
        var activeSessionCount = await _chatSessionRepository.GetActiveSessionCountAsync(userId);
        if (activeSessionCount >= MaxSessionsPerUser)
        {
            throw new InvalidOperationException(
                "Maksimum 5 aktif sohbet sınırına ulaştınız. Lütfen yeni sohbet açmak için var olanlardan birini siliniz.");
        }

        var session = new ChatSession
        {
            AppUserId = userId,
            Title = "Yeni Sohbet",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _chatSessionRepository.AddAsync(session);
        return session;
    }

    public async Task<List<ChatSession>> GetUserSessionsAsync(int userId)
    {
        return await _chatSessionRepository.GetUserSessionsWithMessagesAsync(userId);
    }

    public async Task<ChatSession?> GetSessionWithMessagesAsync(int sessionId, int userId)
    {
        return await _chatSessionRepository.GetSessionWithMessagesAsync(sessionId, userId);
    }

    public async Task DeleteSessionAsync(int sessionId, int userId)
    {
        var session = await GetOwnedSessionAsync(sessionId, userId);
        await _chatSessionRepository.DeleteAsync(session);
    }

    public async Task<ChatMessage> AddUserMessageAsync(int sessionId, string content, int userId)
    {
        var session = await GetOwnedSessionAsync(sessionId, userId);

        var messageCount = await _chatSessionRepository.GetMessageCountInSessionAsync(sessionId);
        if (messageCount >= MaxMessagesPerSession)
        {
            throw new InvalidOperationException(
                "Bu sohbetteki maksimum 5 mesaj sınırına ulaştınız. Lütfen yeni bir sohbet başlatınız.");
        }

        // İlk kullanıcı mesajı ise sohbet başlığını mesajın ilk 25 karakterinden üret.
        if (messageCount == 0)
        {
            var trimmed = content.Trim();
            session.Title = trimmed.Length > 25 ? trimmed[..25] + "..." : trimmed;
            await _chatSessionRepository.UpdateAsync(session);
        }

        var message = new ChatMessage
        {
            ChatSessionId = sessionId,
            Role = "user",
            Content = content,
            SentAt = DateTime.UtcNow
        };

        await _chatSessionRepository.AddMessageAsync(message);
        return message;
    }

    public async Task<ChatMessage> AddAssistantMessageAsync(int sessionId, string content)
    {
        var message = new ChatMessage
        {
            ChatSessionId = sessionId,
            Role = "assistant",
            Content = content,
            SentAt = DateTime.UtcNow
        };

        await _chatSessionRepository.AddMessageAsync(message);
        return message;
    }

    /// <summary>
    /// Oturumu getirir ve yalnızca sahibi olan kullanıcıya ait olduğunu doğrular.
    /// Aksi halde <see cref="InvalidOperationException"/> fırlatır.
    /// </summary>
    private async Task<ChatSession> GetOwnedSessionAsync(int sessionId, int userId)
    {
        var session = await _chatSessionRepository.GetAsync(sessionId);
        if (session is null || session.AppUserId != userId)
        {
            throw new InvalidOperationException("Sohbet oturumu bulunamadı veya bu işlem için yetkiniz yok.");
        }

        return session;
    }
}
