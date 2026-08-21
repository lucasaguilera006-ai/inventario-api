using System.ComponentModel.DataAnnotations.Schema;

namespace InventarioAPI.Models
{
    public class Conversacion
    {
        public int Id { get; set; }
        public string Titulo { get; set; } 
        public string Usuario { get; set; }
        public DateTime FechaInicio { get; set; } = DateTime.UtcNow;

        public ICollection<Mensaje> Mensajes { get; set; } = new List<Mensaje>();
    }
}