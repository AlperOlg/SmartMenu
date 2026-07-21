using Project.Core.Entities;

namespace Project.Business.Abstract;

public interface IChatSessionService
{
    /// <summary>
    /// Kullanıcı için yeni bir sohbet oturumu oluşturur.
    /// Kullanıcının aktif sohbet sayısı 5'e ulaştıysa <see cref="InvalidOperationException"/> fırlatır.
    /// </summary>
    Task<ChatSession> CreateSessionAsync(int userId);

    /// <summary>
    /// Kullanıcıya ait tüm sohbet oturumlarını (mesajları ile birlikte) getirir.
    /// </summary>
    Task<List<ChatSession>> GetUserSessionsAsync(int userId);

    /// <summary>
    /// Belirtilen oturumu, yalnızca sahibi olan kullanıcıya ait olması şartıyla, mesajları ile birlikte getirir.
    /// </summary>
    Task<ChatSession?> GetSessionWithMessagesAsync(int sessionId, int userId);

    /// <summary>
    /// Belirtilen oturumu, yalnızca sahibi olan kullanıcıya ait olması şartıyla siler.
    /// </summary>
    Task DeleteSessionAsync(int sessionId, int userId);

    /// <summary>
    /// Oturuma yeni bir kullanıcı mesajı ekler. Oturumdaki mesaj sayısı 5'e ulaştıysa
    /// <see cref="InvalidOperationException"/> fırlatır. İlk mesajda oturum başlığını günceller.
    /// </summary>
    Task<ChatMessage> AddUserMessageAsync(int sessionId, string content, int userId);

    /// <summary>
    /// Oturuma yeni bir asistan (AI) mesajı ekler.
    /// </summary>
    Task<ChatMessage> AddAssistantMessageAsync(int sessionId, string content);
}
