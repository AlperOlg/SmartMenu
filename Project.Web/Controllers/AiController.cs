using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.Business.Abstract;

namespace Project.Web.Controllers;

[Authorize]
public class AiController : Controller
{
    private readonly IAiService _aiService;

    public AiController(IAiService aiService)
    {
        _aiService = aiService;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> SendMessage(string message)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return Json(new { success = false, message = "Mesaj boş olamaz." });
            }

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var currentUserId))
            {
                return Json(new { success = false, message = "Kullanıcı kimliği doğrulanamadı." });
            }

            var reply = await _aiService.GenerateResponseAsync(message, currentUserId);
            return Json(new { success = true, response = reply });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"AiController.SendMessage hata: {ex}");
            return Json(new { success = false, message = $"Hata: {ex.Message}" });
        }
    }
}
