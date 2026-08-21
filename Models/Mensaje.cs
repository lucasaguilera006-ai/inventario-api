using System.ComponentModel.DataAnnotations.Schema;

namespace InventarioAPI.Models
{
    public class Mensaje
    {
        public int Id { get; set; }
        public string Rol { get; set; } 
        public string Contenido { get; set; }
        public DateTime FechaEnvio { get; set; } = DateTime.UtcNow;
        public int ConversacionId { get; set; }
        public Conversacion Conversacion { get; set; }
    }
}