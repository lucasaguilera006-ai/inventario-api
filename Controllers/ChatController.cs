using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InventarioAPI.Data;
using InventarioAPI.Services;
using InventarioAPI.Models;
using InventarioAPI.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace InventarioAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
private readonly AppDbContext _context;
private readonly GeminiService _geminiService;
private readonly ILogger<ChatController> _logger;

public ChatController(AppDbContext context, GeminiService geminiService, ILogger<ChatController> logger)
{
    _context = context;
    _geminiService = geminiService;
    _logger = logger;
}
[HttpGet]
public async Task<IActionResult> GetConversations()
{
    var username = User.Identity?.Name;
    if (string.IsNullOrEmpty(username))
        return Unauthorized();

    var list = await _context.Conversations
        .AsNoTracking()
        .Where(c => c.User == username)
        .OrderByDescending(c => c.StartDate)
        .Select(c => new { c.Id, c.Title, c.StartDate })
        .ToListAsync();

    return Ok(list);
}

[HttpGet("{id}")]
public async Task<IActionResult> GetConversation(int id)
{
    var username = User.Identity?.Name;
    if (string.IsNullOrEmpty(username))
        return Unauthorized();

    var conversation = await _context.Conversations
        .AsNoTracking()
        .Where(c => c.Id == id && c.User == username)
        .Select(c => new
        {
            c.Id,
            c.Title,
            c.StartDate,
            Messages = c.Messages
                .OrderBy(m => m.Timestamp).ThenBy(m => m.Id)
                .Select(m => new { m.Role, m.Content, m.Timestamp })
        })
        .FirstOrDefaultAsync();

    return conversation == null ? NotFound("Conversation not found.") : Ok(conversation);
}

[HttpPost]
public async Task<ActionResult<ChatResponseDTO>> SendMessage(ChatRequestDTO dto)
{
    if (string.IsNullOrWhiteSpace(dto.Content))
        return BadRequest("The message content cannot be empty.");

    var username = User.Identity?.Name;
    if (string.IsNullOrEmpty(username))
        return Unauthorized();

    Conversation? conversation = null;
    var history = new List<Message>();

    if (dto.ConversationId != null)
    {
        // Filtering by owner: someone else's conversation is a 404
        conversation = await _context.Conversations
            .FirstOrDefaultAsync(c => c.Id == dto.ConversationId && c.User == username);
        if (conversation == null)
            return NotFound("Conversation not found.");

        history = await _context.Messages
            .AsNoTracking()
            .Where(m => m.ConversationId == conversation.Id)
            .OrderBy(m => m.Timestamp).ThenBy(m => m.Id)
            .ToListAsync();
    }

    var userMessage = new Message { Role = "user", Content = dto.Content };
    history.Add(userMessage);

    string reply;
    try
    {
        reply = await _geminiService.SendMessages(history);
    }
    catch (Exception ex) when (!HttpContext.RequestAborted.IsCancellationRequested)
    {
        _logger.LogError(ex, "Gemini request failed (conversation {ConversationId})", dto.ConversationId);

    var status = ex is TaskCanceledException or TimeoutException
        ? StatusCodes.Status504GatewayTimeout
        : StatusCodes.Status502BadGateway;

    return StatusCode(status,
        new { message = "The AI service is currently unavailable. Please try again later." });
}

    if (conversation == null)
    {
        conversation = new Conversation { Title = Truncate(dto.Content), User = username };
        _context.Conversations.Add(conversation);
        await _context.SaveChangesAsync();
    }

    userMessage.ConversationId = conversation.Id;
    var botMessage = new Message { Role = "model", Content = reply };

    if (conversation == null)
    {
        conversation = new Conversation
        {
            Title = Truncate(dto.Content),
            User = username,
            Messages = { userMessage, botMessage }
        };
        _context.Conversations.Add(conversation);
    }
    else
    {
        userMessage.ConversationId = conversation.Id;
        botMessage.ConversationId = conversation.Id;
        _context.Messages.AddRange(userMessage, botMessage);
    }

    // One SaveChanges = one transaction: either everything is saved or nothing
    await _context.SaveChangesAsync();

    return Ok(new ChatResponseDTO { ConversationId = conversation.Id, Reply = reply });
}

private static string Truncate(string s, int max = 60) => s.Length <= max ? s : s[..max];
}
}