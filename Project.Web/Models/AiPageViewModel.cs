using Project.Business.Dtos;

namespace Project.Web.Models;

public class AiPageViewModel
{
    /// <summary>
    /// Kullanıcının sohbetleri (kullanıcı başına en fazla 5 adet).
    /// </summary>
    public List<ChatSessionViewModel> Sessions { get; set; } = new();

    /// <summary>
    /// O an açık olan sohbetin ID'si.
    /// </summary>
    public int CurrentSessionId { get; set; }

    /// <summary>
    /// Açık olan sohbetin mesaj geçmişi.
    /// </summary>
    public List<ChatMessageDto> Messages { get; set; } = new();

    /// <summary>
    /// Kullanıcı sohbet limitine (5) ulaştı mı?
    /// </summary>
    public bool HasReachedSessionLimit => Sessions.Count >= 5;
}
