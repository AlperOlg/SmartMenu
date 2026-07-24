using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Project.Business.Abstract;
using Project.Business.Dtos;
using Project.Core.Entities;
using Project.Web.Models;

namespace Project.Web.Controllers;

[Authorize]
public class AiController : Controller
{
    private readonly IAiService _aiService;
    private readonly IChatSessionService _chatSessionService;
    private readonly ICompositeViewEngine _viewEngine;

    public AiController(
        IAiService aiService,
        IChatSessionService chatSessionService,
        ICompositeViewEngine viewEngine)
    {
        _aiService = aiService;
        _chatSessionService = chatSessionService;
        _viewEngine = viewEngine;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        if (!TryGetUserId(out var userId))
        {
            return Challenge();
        }

        var sessions = await _chatSessionService.GetUserSessionsAsync(userId);

        // Hiç sohbet yoksa otomatik olarak bir tane oluştur.
        if (sessions.Count == 0)
        {
            await _chatSessionService.CreateSessionAsync(userId);
            sessions = await _chatSessionService.GetUserSessionsAsync(userId);
        }

        var currentSession = sessions.First();

        var model = new AiPageViewModel
        {
            Sessions = MapSessions(sessions),
            CurrentSessionId = currentSession.Id,
            Messages = MapMessages(currentSession)
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateNewSession()
    {
        if (!TryGetUserId(out var userId))
        {
            return Json(new { success = false, message = "Kullanıcı kimliği doğrulanamadı." });
        }

        try
        {
            var session = await _chatSessionService.CreateSessionAsync(userId);
            var sessions = await _chatSessionService.GetUserSessionsAsync(userId);
            var sidebarHtml = await RenderPartialToStringAsync("_ChatSidebarPartial", MapSessions(sessions));

            return Json(new
            {
                success = true,
                sessionId = session.Id,
                title = session.Title,
                sidebarHtml
            });
        }
        catch (InvalidOperationException ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SelectSession(int sessionId)
    {
        if (!TryGetUserId(out var userId))
        {
            return Json(new { success = false, message = "Kullanıcı kimliği doğrulanamadı." });
        }

        var session = await _chatSessionService.GetSessionWithMessagesAsync(sessionId, userId);
        if (session is null)
        {
            return Json(new { success = false, message = "Sohbet bulunamadı." });
        }

        var messages = MapMessages(session).Select(m => new
        {
            role = m.IsUser ? "user" : "bot",
            content = m.Content
        });

        return Json(new { success = true, sessionId = session.Id, title = session.Title, messages });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteSession(int sessionId)
    {
        if (!TryGetUserId(out var userId))
        {
            return Json(new { success = false, message = "Kullanıcı kimliği doğrulanamadı." });
        }

        try
        {
            await _chatSessionService.DeleteSessionAsync(sessionId, userId);
        }
        catch (InvalidOperationException ex)
        {
            return Json(new { success = false, message = ex.Message });
        }

        var sessions = await _chatSessionService.GetUserSessionsAsync(userId);

        // Kullanıcının hiç sohbeti kalmadıysa, arayüzün boş kalmaması için yeni bir tane aç.
        if (sessions.Count == 0)
        {
            await _chatSessionService.CreateSessionAsync(userId);
            sessions = await _chatSessionService.GetUserSessionsAsync(userId);
        }

        var sidebarHtml = await RenderPartialToStringAsync("_ChatSidebarPartial", MapSessions(sessions));
        var currentSessionId = sessions.First().Id;

        return Json(new { success = true, sidebarHtml, currentSessionId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendMessage(int sessionId, string message, int? restaurantId = null)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return Json(new { success = false, message = "Mesaj boş olamaz." });
        }

        if (!TryGetUserId(out var userId))
        {
            return Json(new { success = false, message = "Kullanıcı kimliği doğrulanamadı." });
        }

        try
        {
            // 1) Kullanıcı mesajını kaydet (5 mesaj limiti ve başlık güncellemesi servis katmanında uygulanır).
            var userMessage = await _chatSessionService.AddUserMessageAsync(sessionId, message, userId);

            // 2) AI yanıtını üret (restaurantId varsa RAG; yoksa / indeks boşsa genel asistan).
            var reply = await _aiService.GenerateResponseAsync(message, userId, restaurantId);

            // 3) Asistan yanıtını kaydet.
            await _chatSessionService.AddAssistantMessageAsync(sessionId, reply);

            // İlk mesajda başlık değişmiş olabilir; güncel oturumu okuyup başlığı geri gönderelim.
            var session = await _chatSessionService.GetSessionWithMessagesAsync(sessionId, userId);

            return Json(new
            {
                success = true,
                response = reply,
                sessionId,
                title = session?.Title
            });
        }
        catch (InvalidOperationException ex)
        {
            // Örn. "Bu sohbetteki maksimum 5 mesaj sınırına ulaştınız..."
            return Json(new { success = false, limitReached = true, message = ex.Message });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"AiController.SendMessage hata: {ex}");
            return Json(new { success = false, message = $"Hata: {ex.Message}" });
        }
    }

    private bool TryGetUserId(out int userId)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userIdClaim, out userId);
    }

    private static List<ChatSessionViewModel> MapSessions(IEnumerable<ChatSession> sessions)
        => sessions.Select(s => new ChatSessionViewModel
        {
            Id = s.Id,
            Title = s.Title,
            CreatedAt = s.CreatedAt,
            IsActive = s.IsActive
        }).ToList();

    private static List<ChatMessageDto> MapMessages(ChatSession session)
        => (session.Messages ?? new List<ChatMessage>())
            .OrderBy(m => m.SentAt)
            .ThenBy(m => m.Id)
            .Select(m => new ChatMessageDto
            {
                Id = m.Id,
                Role = m.Role,
                Content = m.Content,
                SentAt = m.SentAt
            }).ToList();

    private async Task<string> RenderPartialToStringAsync(string viewName, object model)
    {
        ViewData.Model = model;

        await using var writer = new StringWriter();
        var viewResult = _viewEngine.FindView(ControllerContext, viewName, isMainPage: false);

        if (!viewResult.Success || viewResult.View is null)
        {
            throw new InvalidOperationException($"'{viewName}' partial view bulunamadı.");
        }

        var viewContext = new ViewContext(
            ControllerContext,
            viewResult.View,
            ViewData,
            TempData,
            writer,
            new HtmlHelperOptions());

        await viewResult.View.RenderAsync(viewContext);
        return writer.ToString();
    }
}
