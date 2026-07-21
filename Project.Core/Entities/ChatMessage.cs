using System.ComponentModel.DataAnnotations;

namespace Project.Core.Entities;

public class ChatMessage
{
    public int Id { get; set; }

    public int ChatSessionId { get; set; }
    public ChatSession ChatSession { get; set; } = null!;

    /// <summary>
    /// Mesajın kaynağını belirtir: "user" veya "assistant".
    /// </summary>
    [Required]
    [StringLength(20)]
    public string Role { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;

    public DateTime SentAt { get; set; } = DateTime.UtcNow;
}
