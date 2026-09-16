using crudEbancoDist.Data;
using crudEbancoDist8.DTOs;
using crudEbancoDist8.Models;
using crudEbancoDist8.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using static System.Net.WebRequestMethods;
using crudEbancoDist8.Interfaces;
using Microsoft.AspNetCore.Authorization;
using crudEbancoDist8.Authorization;

namespace crudEbancoDist8.Controllers


{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BebidasController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IBebidasService _bebidasService;

        public BebidasController(
        AppDbContext context,
        IBebidasService bebidasService)
        {
            _context = context;
            _bebidasService = bebidasService;
        }

        
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ReadBebidaDto>>> GetBebidas()
        {
            var bebidas = await _bebidasService.GetAllAsync();

            return Ok(bebidas);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ReadBebidaDto>> GetBebidaById(int id)
        {
            if (id <= 0)
            {
                return BadRequest(new
                {
                    message = "O identificador deve ser maior que zero."
                });
            }

            var bebida = await _bebidasService.GetByIdAsync(id);

            if (bebida is null)
            {
                return NotFound(new
                {
                    message = $"Bebida com o ID {id} não encontrada."
                });
            }

            return Ok(bebida);
        }

        [Authorize(Policy = AppPolicies.GerenciarBebidas)]
        [HttpPost]
        public async Task<ActionResult<ReadBebidaDto>> CreateBebida(
        [FromBody] CreateBebidaDto bebidaDto)
        {
            var bebidaCriada = await _bebidasService.CreateAsync(bebidaDto);

            if (bebidaCriada is null)
                return BadRequest(new
                {
                    message = $"A categoria de ID {bebidaDto.CategoryId} não existe."
                });
            return CreatedAtAction(
                nameof(GetBebidaById),
                new { id = bebidaCriada.Id },
                bebidaCriada
            );

        }

        [Authorize(Policy = AppPolicies.GerenciarBebidas)]
        [HttpPost("lote")]
        public async Task<IActionResult> CreateBebidasInLote([FromBody] List<CreateBebidaDto> dtos)
        {
            if (dtos == null || !dtos.Any())
            {
                return BadRequest(new { message = "Lista de bebidas inválida" });
            }
            var resultado = await _bebidasService.CreateBebidasInLote(dtos);
            if (resultado is null)
            {
                return BadRequest(new { message = "Uma ou mais bebidas não puderam ser criadas." });
            }
            return StatusCode(StatusCodes.Status201Created, resultado);
        }

        [Authorize(Policy = AppPolicies.GerenciarBebidas)]
        [HttpPut("{id:int}")]
        public async Task<ActionResult<ReadBebidaDto>> UpdateBebida(
        int id,
        [FromBody] UpdateBebidaDto updateBebidaDto)
        {
            if (id <= 0)
            {
                return BadRequest(new
                {
                    message = "O identificador deve ser maior que zero."
                });
            }
            var resultado = await _bebidasService.UpdateAsync(id, updateBebidaDto);

            if (resultado.Erro == "bebida_nao_encontrada")
            {
                return NotFound(new
                {
                    message = $"Bebida com o ID {id} não encontrada."
                });
            }

            if (resultado.Erro == "categoria_nao_encontrada")
            {
                return BadRequest(new
                {
                    message = $"A categoria de ID {updateBebidaDto.CategoryId} não existe."
                });
            }

            return Ok(resultado.Bebida);


        }

        [Authorize(Policy = AppPolicies.GerenciarBebidas)]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteBebida(int id)
        {
            if (id <= 0)
            {
                return BadRequest(new
                {
                    message = "O identificador deve ser maior que zero."
                });
            }

            var excluida = await _bebidasService.DeleteAsync(id);

            if (!excluida)
            {
                return NotFound(new
                {
                    message = $"Bebida com o ID {id} não encontrada."
                });
            }

            return NoContent();
        }


        [HttpGet("paginado")]
        public async Task<ActionResult<PagedResult<ReadBebidaDto>>>
        GetBebidasPaginadas(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string orderBy = "id",
        [FromQuery] string direction = "asc",
        [FromQuery] string? name = null,
        [FromQuery] int? categoryId = null,
        [FromQuery] decimal? minPrice = null,
        [FromQuery] decimal? maxPrice = null)
        {
            if (pageNumber <= 0)
            {
                return BadRequest(new
                {
                    message = "O número da página deve ser maior que zero."
                });
            }

            if (pageSize <= 0 || pageSize > 100)
            {
                return BadRequest(new
                {
                    message = "O tamanho da página deve estar entre 1 e 100."
                });
            }

            orderBy = orderBy.Trim().ToLowerInvariant();
            direction = direction.Trim().ToLowerInvariant();

            string[] camposPermitidos =
            {
        "id",
        "name",
        "price",
        "quantity"
    };

            if (!camposPermitidos.Contains(orderBy))
            {
                return BadRequest(new
                {
                    message = "OrderBy deve ser: id, name, price ou quantity."
                });
            }

            if (direction != "asc" && direction != "desc")
            {
                return BadRequest(new
                {
                    message = "Direction deve ser asc ou desc."
                });
            }

            if (categoryId.HasValue && categoryId.Value <= 0)
            {
                return BadRequest(new
                {
                    message = "O identificador da categoria deve ser maior que zero."
                });
            }

            if (minPrice.HasValue && minPrice.Value < 0)
            {
                return BadRequest(new
                {
                    message = "O preço mínimo não pode ser negativo."
                });
            }

            if (maxPrice.HasValue && maxPrice.Value < 0)
            {
                return BadRequest(new
                {
                    message = "O preço máximo não pode ser negativo."
                });
            }

            if (minPrice.HasValue &&
                maxPrice.HasValue &&
                minPrice.Value > maxPrice.Value)
            {
                return BadRequest(new
                {
                    message = "O preço mínimo não pode ser maior que o máximo."
                });
            }

            var resultado = await _bebidasService.GetPaginadoAsync(
                pageNumber,
                pageSize,
                orderBy,
                direction,
                name,
                categoryId,
                minPrice,
                maxPrice
            );

            return Ok(resultado);
        }


    }
}
