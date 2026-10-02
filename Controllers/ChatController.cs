using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InventarioAPI.Data;
using InventarioAPI.Services;
using InventarioAPI.Models;
using InventarioAPI.DTOs;
using Microsoft.AspNetCore.Authorization;

namespace InventarioAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly GeminiService _geminiService;

        public ChatController(AppDbContext context, GeminiService geminiService)
        {
            _context = context;
            _geminiService = geminiService;
        }
[HttpPost]
public async Task<ActionResult<ChatResponseDTO>> SendMessage(ChatRequestDTO dto)
{
    if (string.IsNullOrWhiteSpace(dto.Content))
    {
        return BadRequest("The message content cannot be empty.");
    }

    Conversacion? conversation;

    if (dto.ConversationId == null)
    {
        conversation = new Conversacion
        {
            Titulo = dto.Content,
            Usuario = User.Identity?.Name ?? "guest"
        };
        _context.Conversaciones.Add(conversation);
        await _context.SaveChangesAsync();
    }
    else
    {
        conversation = await _context.Conversaciones.FindAsync(dto.ConversationId);
        if (conversation == null)
        {
            return NotFound("Conversation not found.");
        }
    }

    var userMessage = new Mensaje
    {
        Rol = "user",
        Contenido = dto.Content,
        ConversacionId = conversation.Id
    };
    _context.Mensajes.Add(userMessage);
    await _context.SaveChangesAsync();

    var history = await _context.Mensajes
        .Where(m => m.ConversacionId == conversation.Id)
        .OrderBy(m => m.FechaEnvio)
        .ToListAsync();

    var reply = await _geminiService.EnviarMensaje(history);

    var botMessage = new Mensaje
    {
        Rol = "model",
        Contenido = reply,
        ConversacionId = conversation.Id
    };
    _context.Mensajes.Add(botMessage);
    await _context.SaveChangesAsync();

    return Ok(new ChatResponseDTO
    {
        ConversationId = conversation.Id,
        Reply = reply
    });
}
}
}