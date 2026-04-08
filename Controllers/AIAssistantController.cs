using System.Security.Claims;
using Dashboard.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dashboard.Controllers;

[Authorize]
[Route("AIAssistant")]
public class AIAssistantController : Controller
{
    private readonly IAIChatService _chatService;
    private readonly ILogger<AIAssistantController> _logger;

    public AIAssistantController(IAIChatService chatService, ILogger<AIAssistantController> logger)
    {
        _chatService = chatService;
        _logger = logger;
    }

    public IActionResult Index()
    {
        var userId = GetUserId();
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "Unknown";
        ViewData["UserRole"] = userRole;
        return View();
    }

    private string GetUserId() =>
        User.FindFirst("UserId")?.Value
        ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? string.Empty;
}
