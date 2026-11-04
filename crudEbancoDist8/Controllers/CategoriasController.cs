using crudEbancoDist.Data;
using crudEbancoDist8.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static System.Net.WebRequestMethods;
using crudEbancoDist8.DTOs;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using crudEbancoDist8.Authorization;


namespace crudEbancoDist8.Controllers

{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class CategoriasController : ControllerBase
    {

        private readonly AppDbContext _context;

        public CategoriasController(AppDbContext context)
        {
            _context = context;
        }

        [Authorize(Roles = AppRoles.Admin)]
        [HttpGet]


        public async Task<ActionResult<IEnumerable<ReadCategoriasDto>>> GetCategorias()
        {
            var categorias = await _context.Categorias
                .Select(c => new ReadCategoriasDto { Name = c.Name, Id = c.Id })
                .ToListAsync();
            return categorias;
        }

        [Authorize(Roles = AppRoles.Admin)]
        [HttpPost]

        public async Task<IActionResult> CreateCategoria([FromBody] CreateCategoriasDto categoriaDto)
        {
            var categoria = new Categoria { Name = categoriaDto.Name };
            _context.Categorias.Add(categoria);
            await _context.SaveChangesAsync();
            return Ok(categoria);
        }

        [Authorize(Roles = AppRoles.Admin)]
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

        [Authorize(Roles = AppRoles.Admin)]
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

        [Authorize(Roles = AppRoles.Admin)]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteCategoria(int id)
        {
            if (id <= 0)
            {
                return BadRequest(new
                {
                    message = "O identificador deve ser maior que zero."
                });
            }

            var categoria = await _context.Categorias
                .FirstOrDefaultAsync(c => c.Id == id);

            if (categoria is null)
            {
                return NotFound(new
                {
                    message = "Categoria não encontrada."
                });
            }

            var possuiBebidas = await _context.Bebidas
                .AnyAsync(b => b.CategoryId == id);

            if (possuiBebidas)
            {
                return Conflict(new
                {
                    message =
                        "Não é possível excluir uma categoria que possui bebidas."
                });
            }

            _context.Categorias.Remove(categoria);
            await _context.SaveChangesAsync();

            return NoContent();
        }

    }
}
