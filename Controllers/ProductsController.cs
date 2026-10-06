using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InventarioAPI.Data;
using InventarioAPI.DTOs;
using InventarioAPI.Models;

namespace InventarioAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProductsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProductResponseDTO>>> GetProducts()
        {
            var products = await _context.Productos.AsNoTracking().ToListAsync();
            return Ok(products.Select(ToResponse));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ProductResponseDTO>> GetProduct(int id)
        {
            var product = await _context.Productos.FindAsync(id);
            if (product == null) return NotFound();
            return ToResponse(product);
        }

        [HttpPost]
        public async Task<ActionResult<ProductResponseDTO>> CreateProduct(ProductDTO dto)
        {
            var product = new Producto
            {
                Nombre = dto.Name,
                Descripcion = dto.Description,
                Precio = dto.Price,
                Stock = dto.Stock,
                Categoria = dto.Category
            };
            _context.Productos.Add(product);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, ToResponse(product));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(int id, ProductDTO dto)
        {
            var product = await _context.Productos.FindAsync(id);
            if (product == null) return NotFound();

            product.Nombre = dto.Name;
            product.Descripcion = dto.Description;
            product.Precio = dto.Price;
            product.Stock = dto.Stock;
            product.Categoria = dto.Category;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Productos.FindAsync(id);
            if (product == null) return NotFound();

            _context.Productos.Remove(product);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        private static ProductResponseDTO ToResponse(Producto p)
        {
            return new ProductResponseDTO
            {
                Id = p.Id,
                Name = p.Nombre,
                Description = p.Descripcion,
                Price = p.Precio,
                Stock = p.Stock,
                Category = p.Categoria,
            };
        }
    }
}