using System.ComponentModel.DataAnnotations.Schema;

namespace InventarioAPI.Models
{
    public class Producto
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        [Column(TypeName = "decimal(18,2)")]
        public decimal Precio { get; set; }
        public int Stock { get; set; }
        public string Categoria { get; set; }
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    }
}