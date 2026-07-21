using Project.Core.Entities;

namespace Project.DataAccess.Abstract;

public interface IChatSessionRepository : IGenericRepository<ChatSession>
{
    /// <summary>
    /// Belirtilen oturumu, sahibi olan kullanıcıya ait olması şartıyla,
    /// mesajları kronolojik sırada (SentAt/Id ascending) dahil ederek getirir.
    /// </summary>
    Task<ChatSession?> GetSessionWithMessagesAsync(int sessionId, int userId);

    /// <summary>
    /// Kullanıcıya ait tüm oturumları, mesajları ile birlikte getirir.
    /// </summary>
    Task<List<ChatSession>> GetUserSessionsWithMessagesAsync(int userId);

    /// <summary>
    /// Kullanıcının aktif toplam sohbet oturumu sayısını döner.
    /// </summary>
    Task<int> GetActiveSessionCountAsync(int userId);

    /// <summary>
    /// Belirtilen sohbet oturumundaki toplam mesaj sayısını döner.
    /// </summary>
    Task<int> GetMessageCountInSessionAsync(int sessionId);

    /// <summary>
    /// Sohbete yeni bir mesaj ekler ve kalıcı hale getirir.
    /// </summary>
    Task AddMessageAsync(ChatMessage message);
}
