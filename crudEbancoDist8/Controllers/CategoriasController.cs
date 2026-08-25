using crudEbancoDist.Data;
using crudEbancoDist8.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static System.Net.WebRequestMethods;
using crudEbancoDist8.DTOs;
using System.ComponentModel.DataAnnotations;
namespace crudEbancoDist8.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriasController : ControllerBase
    {

        private readonly AppDbContext _context;

        public CategoriasController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]


        public async Task<ActionResult<IEnumerable<ReadCategoriasDto>>> GetCategorias()
        {
            var categorias = await _context.Categorias
                .Select(c => new ReadCategoriasDto { Name = c.Name, Id = c.Id })
                .ToListAsync();
            return categorias;
        }


        [HttpPost]

        public async Task<IActionResult> CreateCategoria([FromBody] CreateCategoriasDto categoriaDto)
        {
            var categoria = new Categoria { Name = categoriaDto.Name };
            _context.Categorias.Add(categoria);
            await _context.SaveChangesAsync();
            return Ok(categoria);
        }

        [HttpGet("{id}")]

        public async Task<ActionResult<ReadCategoriasDto>> GetCategoria(int id)
        {
            var categoria = await _context.Categorias
                .Where(c => c.Id == id)
                .Select(c => new ReadCategoriasDto { Name = c.Name, Id = c.Id })
                .FirstOrDefaultAsync();


            if (categoria == null)
            {
                return NotFound();
            }
            return Ok(categoria);
        }

        [HttpPut("{id}")]

        public async Task<IActionResult> UpdateCategoria(int id, [FromBody] UpdateCategoriasDto categoriaDto)
        {
            var categoria = await _context.Categorias.FindAsync(id);
            if (categoria == null)
            {
                return NotFound();
            }
            categoria.Name = categoriaDto.Name;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategoria(int id)
        {
            var categoria = await _context.Categorias.FindAsync(id);
            if (categoria == null)
            {
                return NotFound();
            }
            _context.Categorias.Remove(categoria);
            await _context.SaveChangesAsync();
            return NoContent();
        }

    }
}
