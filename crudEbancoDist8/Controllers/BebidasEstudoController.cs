using crudEbancoDist.Data;
using crudEbancoDist8.DTOs;
using crudEbancoDist8.Models;
using crudEbancoDist8.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using static System.Net.WebRequestMethods;
using crudEbancoDist8.Interfaces;

namespace crudEbancoDist8.Controllers


{
    [ApiController]
    [Route("api/[controller]")]
    public class BebidasEstudoController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IBebidasService _bebidasService;

        public BebidasEstudoController(
        AppDbContext context,
        IBebidasService bebidasService)
        {
            _context = context;
            _bebidasService = bebidasService;
        }



        [HttpPost("lote")]
        public async Task<IActionResult> CreateBebidasInLote([FromBody] List<CreateBebidaDto> dtos)
        {
            if (dtos == null || !dtos.Any())
            {
                return BadRequest(new { message = "Lista de bebidas inválida" });
            }
            var bebidas = dtos.Select(dto => new Bebidas
            {
                Name = dto.Name,
                Price = dto.Price,
                Quantity = dto.Quantity,
                CategoryId = dto.CategoryId
            }).ToList();
            _context.Bebidas.AddRange(bebidas);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Bebidas criadas com sucesso", bebidas });
        }


        [HttpPut("{id:int}")]
        public async Task<ActionResult<ReadBebidaDto>> UpdateBebida(
        int id,
        [FromBody] UpdateBebidaDto bebidaDto)
        {
            if (id <= 0)
            {
                return BadRequest(new
                {
                    message = "O identificador deve ser maior que zero."
                });
            }
            var resultado = await _bebidasService.UpdateAsync(id, bebidaDto);

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
                    message = $"A categoria de ID {bebidaDto.CategoryId} não existe."
                });
            }

            return Ok(resultado.Bebida);


        }

        [HttpGet("filter")]
        public async Task<IActionResult> FilterBebidas([FromQuery] string category)
        {
            var bebidas = await _context.Bebidas.Where(b => b.CategoryId.ToString() == category).ToListAsync();
            if (string.IsNullOrEmpty(category))
            {
                return BadRequest(new { message = "Categoria inválida" });
            }
            return Ok(new
            {
                message = "Resultados do filtro",
                bebidas
            });

        }
        [HttpGet("filter/preco")]

        public async Task<IActionResult> FilterBebidasByPrice([FromQuery] decimal minPrice, [FromQuery] decimal maxPrice)
        {

            if (minPrice < 0 || maxPrice < 0 || minPrice > maxPrice)
            {
                return BadRequest(new { message = "Intervalo de preço inválido" });
            }
            var bebidas = await _context.Bebidas.Where(b => b.Price >= minPrice && b.Price <= maxPrice).ToListAsync();
            if (!bebidas.Any())
            {
                return NotFound(new { message = "Nenhuma bebida encontrada no intervalo de preço especificado" });
            }

            return Ok(new
            {
                message = "Resultados do filtro por preço",
                bebidas
            });

        }
        [HttpGet("filter/category/price")]
        public async Task<IActionResult> CategoryAndPriceMinMax([FromQuery] decimal minPrice, [FromQuery] decimal maxPrice, [FromQuery] string category)
        {

            if (minPrice < 0 || maxPrice < 0 || minPrice > maxPrice || string.IsNullOrEmpty(category))
            {
                return BadRequest(new { message = "Parâmetros inválidos" });
            }
            var bebidas = await _context.Bebidas
                .Where(b => b.Price >= minPrice && b.Price <= maxPrice && b.CategoryId.ToString() == category.Trim())
                .ToListAsync();
            if (!bebidas.Any())
            {
                return NotFound(new { message = "Nenhuma bebida encontrada com os parâmetros especificados" });
            }
            return Ok(new
            {
                message = "Resultados do filtro por categoria e preço",
                bebidas
            });
        }



        [HttpGet("filtertop5")]
        public async Task<IActionResult> FilterTop5BebidasByPrice([FromQuery] decimal minPrice, [FromQuery] decimal maxPrice)
        {
            if (minPrice < 0 || maxPrice < 0 || minPrice > maxPrice)
            {

                return BadRequest(new { message = "Intervalo de preço inválido" });

            }
            var bebidas = await _context.Bebidas
                .Where(b => b.Price >= minPrice && b.Price <= maxPrice)
                .OrderByDescending(b => b.Price)
                .Take(5)
                .ToListAsync();
            if (!bebidas.Any())
            {
                return NotFound(new { message = "Nenhuma bebida encontrada no intervalo de preço especificado" });
            }
            return Ok(new
            {
                message = "Top 5 bebidas mais caras no intervalo de preço especificado",
                bebidas
            });
        }

        [HttpGet("QuantidadePorCategoria")]
        public async Task<IActionResult> GetQuantidadeBebidasPorCategoria()
        {
            var quantidadePorCategoria = await _context.Bebidas
                .GroupBy(b => b.CategoryId)
                .Select(g => new { Category = g.Key, Quantity = g.Count() })
                .ToListAsync();

            return Ok(new
            {
                message = "Quantidade de bebidas por categoria",
                quantidadePorCategoria
            });
        }


        [HttpGet("Estoque<{10}")]

        public async Task<IActionResult> GetEstoqueMenorqueDez(string id)
        {

            var bebidas = await _context.Bebidas.Where(b => b.Quantity < 10).ToListAsync();
            if (!bebidas.Any())
            {
                return NotFound(new { message = "Nenhuma bebida encontrada com quantidade menor que 10" });
            }
            return Ok(new
            {
                message = "Bebidas com quantidade menor que 10",
                bebidas
            });
        }

        [HttpGet("BuscarPorNome")]
        public async Task<ActionResult<IEnumerable<ReadBebidaDto>>> BuscarPorNome([FromQuery] string nome)
        {
            if (string.IsNullOrEmpty(nome))
            {
                return BadRequest(new { message = "Nome inválido" });
            }
            var bebidas = await _context.Bebidas
                .Where(b => b.Name.ToLower().Contains(nome.ToLower()))
                .Select(b => new ReadBebidaDto
                {
                    Id = b.Id,
                    Name = b.Name,
                    Price = b.Price,
                    Quantity = b.Quantity,
                    Category = b.Category.Name
                })
                .ToListAsync();
            if (!bebidas.Any())
            {
                return NotFound(new { message = "Nenhuma bebida encontrada com o nome especificado" });
            }
            return Ok(new
            {
                message = "Resultados da busca por nome",
                bebidas
            });
        }


        [HttpGet("OrdenarValoresMaiorparaMenor")]
        public async Task<ActionResult> OrdenarBebidasPorValores()
        {
            var bebidas = await _context.Bebidas
                .OrderByDescending(b => b.Price)
                .Select(b => new ReadBebidaDto
                {
                    Id = b.Id,
                    Name = b.Name,
                    Price = b.Price,
                    Quantity = b.Quantity,
                    Category = b.Category.Name
                })
                .ToListAsync();
            return Ok(new
            {
                message = "Bebidas ordenadas por valor",
                bebidas
            });
        }

        [HttpGet("OrdenarMenorParaMaior")]

        public async Task<ActionResult> OrdenarBebidasPorValoresMenorParaMaior()
        {
            var bebidas = await _context.Bebidas
                .OrderBy(b => b.Price)
                .Select(b => new ReadBebidaDto
                {
                    Id = b.Id,
                    Name = b.Name,
                    Price = b.Price,
                    Quantity = b.Quantity,
                    Category = b.Category.Name
                })
                .ToListAsync();
            return Ok(new
            {
                message = "Bebidas ordenadas por valor (menor para maior)",
                bebidas
            });
        }
        [HttpGet("skip-take")]
        public async Task<ActionResult<IEnumerable<ReadBebidaDto>>> GetBebidasSkipTake(
            [FromQuery] int skip = 0,
            [FromQuery] int take = 10)
        {
            if (skip < 0)
            {
                return BadRequest(new
                {
                    message = "Skip não pode ser negativo"
                });
            }

            if (take <= 0 || take > 100)
            {
                return BadRequest(new
                {
                    message = "Take deve estar entre 1 e 100"
                });
            }

            var bebidas = await _context.Bebidas
                .AsNoTracking()
                .OrderBy(b => b.Id)
                .Skip(skip)
                .Take(take)
                .Select(b => new ReadBebidaDto
                {
                    Id = b.Id,
                    Name = b.Name,
                    Price = b.Price,
                    Quantity = b.Quantity,
                    Category = b.Category.Name
                })
                .ToListAsync();

            return Ok(new
            {
                message = "Bebidas consultadas com Skip e Take",
                skip,
                take,
                quantidadeRetornada = bebidas.Count,
                bebidas
            });
        }

        [HttpGet("paginado-skip-take")]
        public async Task<ActionResult> GetBebidasPaginadas([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            if (pageNumber <= 0)
            {
                return BadRequest(new
                {
                    message = "O número da página deve ser maior que zero"
                });
            }

            if (pageSize <= 0 || pageSize > 100)
            {
                return BadRequest(new
                {
                    message = "O tamanho da página deve estar entre 1 e 100"
                });
            }

            var totalRegistros = await _context.Bebidas.CountAsync();

            var totalPaginas = (int)Math.Ceiling(
                totalRegistros / (double)pageSize
            );

            var bebidas = await _context.Bebidas
                .AsNoTracking()
                .OrderBy(b => b.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(b => new ReadBebidaDto
                {
                    Id = b.Id,
                    Name = b.Name,
                    Price = b.Price,
                    Quantity = b.Quantity,
                    Category = b.Category.Name
                })
                .ToListAsync();

            return Ok(new
            {
                pageNumber,
                pageSize,
                totalRegistros,
                totalPaginas,
                temPaginaAnterior = pageNumber > 1,
                temProximaPagina = pageNumber < totalPaginas,
                dados = bebidas
            });
        }

    }
}
