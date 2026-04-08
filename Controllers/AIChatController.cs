using System.Security.Claims;
using Dashboard.Models;
using Dashboard.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Dashboard.Controllers;

[Authorize]
[Route("api/aichat")]
[ApiController]
public class AIChatController : ControllerBase
{
    private readonly IAIChatService _chatService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<AIChatController> _logger;

    public AIChatController(
        IAIChatService chatService,
        UserManager<ApplicationUser> userManager,
        ILogger<AIChatController> logger)
    {
        _chatService = chatService;
        _userManager = userManager;
        _logger = logger;
    }

    private string? GetUserId() =>
        _userManager.GetUserId(User)
        ?? User.FindFirst("UserId")?.Value
        ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    [HttpPost("sessions")]
    public async Task<IActionResult> CreateSession([FromBody] CreateSessionRequest? request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Department))
            return BadRequest(new { error = "Department is required." });

        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("CreateSession rejected: no user id in claims.");
            return Unauthorized(new { error = "Not authenticated." });
        }

        try
        {
            var session = await _chatService.CreateSessionAsync(userId, request.Department.Trim());
            return Ok(session);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateSession failed for user {UserId}", userId);
            return StatusCode(500, new { error = "Could not create chat session." });
        }
    }

    [HttpGet("sessions")]
    public async Task<IActionResult> GetSessions([FromQuery] string? department = null)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var sessions = await _chatService.GetSessionsAsync(userId, department);
        return Ok(sessions);
    }

    [HttpGet("sessions/{sessionId}/messages")]
    public async Task<IActionResult> GetMessages(int sessionId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var messages = await _chatService.GetChatHistoryAsync(sessionId, page, pageSize);
        return Ok(messages);
    }

    [HttpDelete("sessions/{sessionId}")]
    public async Task<IActionResult> DeleteSession(int sessionId)
    {
        var userId = GetUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var success = await _chatService.DeleteSessionAsync(sessionId, userId);
        if (!success)
            return NotFound(new { error = "Session not found or access denied." });

        return Ok(new { success = true });
    }

    [HttpGet("departments")]
    public IActionResult GetDepartments()
    {
        var departments = new[]
        {
            new { value = "sales", label = "Sales", icon = "bx-trending-up", color = "success" },
            new { value = "finance", label = "Finance", icon = "bx-wallet", color = "info" },
            new { value = "marketing", label = "Marketing", icon = "bx-megaphone", color = "warning" },
            new { value = "inventory", label = "Inventory", icon = "bx-package", color = "primary" },
            new { value = "hr", label = "Human Resources", icon = "bx-user", color = "danger" },
            new { value = "cskh", label = "Customer Service", icon = "bx-headphone", color = "secondary" },
            new { value = "executive", label = "Executive", icon = "bx-bar-chart-alt-2", color = "dark" }
        };

        return Ok(departments);
    }
}

public class CreateSessionRequest
{
    public string Department { get; set; } = string.Empty;
}
