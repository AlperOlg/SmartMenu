namespace Project.Core.Entities;

public class ChatSession
{
    public int Id { get; set; }

    public string Title { get; set; } = "Yeni Sohbet";

    public int AppUserId { get; set; }
    public AppUser AppUser { get; set; } = null!;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
}
