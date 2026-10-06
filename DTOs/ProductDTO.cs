using System.ComponentModel.DataAnnotations;

namespace InventarioAPI.DTOs
{
    public class ProductDTO
    {
        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;
        [Range(0.01, 999999999)]
        public decimal Price { get; set; }
        [Range(0, 1000000)]
        public int Stock { get; set; }
        [MaxLength(50)]
        public string Category { get; set; } = string.Empty; 
    }
}