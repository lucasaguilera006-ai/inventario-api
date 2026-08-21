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
        public async Task<ActionResult<ChatResponseDTO>> EnviarMensaje(ChatRequestDTO dto)
        {
            if(string.IsNullOrWhiteSpace(dto.Contenido))
            {
                return BadRequest("El contenido del mensaje no puede estar vacío.");
            }


            Conversacion conversacion;

            if (dto.ConversacionId == null)
            {
                conversacion = new Conversacion
                {
                    Titulo = dto.Contenido,
                    Usuario = User.Identity?.Name ?? "invitado"
                };
                _context.Conversaciones.Add(conversacion);
                await _context.SaveChangesAsync();
            }
            else
            {

                conversacion = await _context.Conversaciones.FindAsync(dto.ConversacionId);
                if (conversacion == null)
                {
                    return NotFound("Conversación no encontrada");
                }

            }

            Mensaje mensaje = new Mensaje
            {
                Rol = "user",
                Contenido = dto.Contenido,
                ConversacionId = conversacion.Id
            };
            _context.Mensajes.Add(mensaje);
            await _context.SaveChangesAsync();

            var historial = await _context.Mensajes
            .Where(m => m.ConversacionId == conversacion.Id)
            .OrderBy(m => m.FechaEnvio)
            .ToListAsync();

            var respuesta = await _geminiService.EnviarMensaje(historial);
            Mensaje mensajeBot = new Mensaje
            {
                Rol = "model",
                Contenido = respuesta,
                ConversacionId = conversacion.Id
            };
            _context.Mensajes.Add(mensajeBot);
            await _context.SaveChangesAsync();

            var responseDTO = new ChatResponseDTO
            {
                ConversacionId = conversacion.Id,
                Respuesta = respuesta
            };
            return Ok(responseDTO);

        }       
    }
}