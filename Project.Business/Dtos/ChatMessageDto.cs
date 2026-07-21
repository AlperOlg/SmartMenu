namespace Project.Business.Dtos;

public class ChatMessageDto
{
    public int Id { get; set; }

    /// <summary>
    /// Mesajın kaynağı: "user" veya "assistant".
    /// </summary>
    public string Role { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public DateTime SentAt { get; set; }

    public bool IsUser => string.Equals(Role, "user", StringComparison.OrdinalIgnoreCase);
}
