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
        private readonly IBebidasService _bebidasService;

        public BebidasController(IBebidasService bebidasService)
        {
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
            GetBebidasPaginadas([FromQuery] BebidasQueryDto filtros)
        {
            if (filtros.PageNumber <= 0)
            {
                return BadRequest(new
                {
                    message = "O número da página deve ser maior que zero."
                });
            }

            if (filtros.PageSize <= 0 || filtros.PageSize > 100)
            {
                return BadRequest(new
                {
                    message = "O tamanho da página deve estar entre 1 e 100."
                });
            }


            filtros.OrderBy =
                (filtros.OrderBy ?? "").Trim().ToLowerInvariant();

            filtros.Direction =
                (filtros.Direction ?? "").Trim().ToLowerInvariant();

            string[] camposPermitidos =
            {
                "id",
                "name",
                "price",
                "quantity"
    };

            if (!camposPermitidos.Contains(filtros.OrderBy))
            {
                return BadRequest(new
                {
                    message = "OrderBy deve ser: id, name, price ou quantity."
                });
            }

            if (filtros.Direction != "asc" && filtros.Direction != "desc")
            {
                return BadRequest(new
                {
                    message = "Direction deve ser asc ou desc."
                });
            }

            if (filtros.CategoryId.HasValue && filtros.CategoryId.Value <= 0)
            {
                return BadRequest(new
                {
                    message = "O identificador da categoria deve ser maior que zero."
                });
            }

            if (filtros.MinPrice.HasValue && filtros.MinPrice.Value < 0)
            {
                return BadRequest(new
                {
                    message = "O preço mínimo não pode ser negativo."
                });
            }

            if (filtros.MaxPrice.HasValue && filtros.MaxPrice.Value < 0)
            {
                return BadRequest(new
                {
                    message = "O preço máximo não pode ser negativo."
                });
            }

            if (filtros.MinPrice.HasValue &&
                filtros.MaxPrice.HasValue &&
                filtros.MinPrice.Value > filtros.MaxPrice.Value)
            {
                return BadRequest(new
                {
                    message = "O preço mínimo não pode ser maior que o máximo."
                });
            }

            if (filtros.MinQuantity.HasValue && filtros.MinQuantity.Value < 0)
            {
                return BadRequest(new
                {
                    message = "A quantidade mínima não pode ser negativa."
                });
            }

            if (filtros.MaxQuantity.HasValue && filtros.MaxQuantity.Value < 0)
            {
                return BadRequest(new
                {
                    message = "A quantidade máxima não pode ser negativa."
                });
            }

            if (filtros.MinQuantity.HasValue &&
                filtros.MaxQuantity.HasValue &&
                filtros.MinQuantity.Value > filtros.MaxQuantity.Value)
            {
                return BadRequest(new
                {
                    message = "A quantidade mínima não pode ser maior que a máxima."
                });
            }

            var resultado =
                await _bebidasService.GetPaginadoAsync(filtros); 
            

            return Ok(resultado);
        }


    }
}
