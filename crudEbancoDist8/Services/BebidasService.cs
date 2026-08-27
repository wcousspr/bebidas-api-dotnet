using crudEbancoDist.Data;
using crudEbancoDist8.DTOs;
using Microsoft.EntityFrameworkCore;
using crudEbancoDist8.Models;
using crudEbancoDist8.Controllers;
using crudEbancoDist8.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;

namespace crudEbancoDist8.Services;

public class BebidasService : IBebidasService
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<BebidasService> _logger;
    public BebidasService(AppDbContext context, IMapper mapper, ILogger<BebidasService> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<List<ReadBebidaDto>> GetAllAsync()
    {
        var bebidas = await _context.Bebidas
            .AsNoTracking()
            .OrderBy(b => b.Id)
            .ProjectTo<ReadBebidaDto>(
                _mapper.ConfigurationProvider)
            .ToListAsync();

        return bebidas;
    }

    public async Task<ReadBebidaDto?> GetByIdAsync(int id)
    {
        var bebida = await _context.Bebidas
            .AsNoTracking()
            .Where(b => b.Id == id)
            .ProjectTo<ReadBebidaDto>(
                _mapper.ConfigurationProvider)
            .FirstOrDefaultAsync();

        return bebida;
    }


    public async Task<ReadBebidaDto?> CreateAsync(CreateBebidaDto createBebidaDto)
    {
        var categoria = await _context.Categorias
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == createBebidaDto.CategoryId);

        if (categoria == null)
        {
            _logger.LogWarning("Tentativa de criar bebida com categoria inexistente. CategoryId: {CategoryId}", createBebidaDto.CategoryId);

            return null;
        }

        var bebida = _mapper.Map<Bebidas>(createBebidaDto);
        

        _context.Bebidas.Add(bebida);
        await _context.SaveChangesAsync();
        _logger.LogInformation(
        "Bebida criada. BebidaId: {BebidaId}, CategoryId: {CategoryId}",
        bebida.Id,
        bebida.CategoryId);


        return new ReadBebidaDto
        {
            Id = bebida.Id,
            Name = bebida.Name,
            Price = bebida.Price,
            Quantity = bebida.Quantity,
            Category = categoria.Name
        };
    }

    public async Task<(ReadBebidaDto? Bebida, string? Erro)> UpdateAsync(int id, UpdateBebidaDto updateBebidaDto)
    {


        var bebida = await _context.Bebidas
        .FirstOrDefaultAsync(b => b.Id == id);

        if (bebida == null)
        {
            return (null, "bebida_nao_encontrada");
        }

        var categoria = await _context.Categorias
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == updateBebidaDto.CategoryId);

        if (categoria == null) {

            _logger.LogWarning(
            "Tentativa de atualizar bebida inexistente. BebidaId: {BebidaId}",
            id
        );


            return (null, "categoria_nao_encontrada"); }

        _mapper.Map(updateBebidaDto, bebida);

        bebida.Name = bebida.Name.Trim();

        await _context.SaveChangesAsync();
        _logger.LogInformation(
        "Bebida atualizada. BebidaId: {BebidaId}, CategoryId: {CategoryId}, Name: {Name}, Quantity: {Quantity}, Price: {Price}",
        bebida.Id,
        bebida.CategoryId,
        bebida.Name,
        bebida.Quantity,
        bebida.Price);

        var updatedBebidaDto = new ReadBebidaDto
        {
            Id = bebida.Id,
            Name = bebida.Name,
            Price = bebida.Price,
            Quantity = bebida.Quantity,
            Category = categoria.Name
        };

        return (updatedBebidaDto, null);

    }

    public async Task<List<ReadBebidaDto>?> CreateBebidasInLote(
    List<CreateBebidaDto> dtos)
    {
        // IDs distintos das categorias recebidas
        var categoryIds = dtos
            .Select(dto => dto.CategoryId)
            .Distinct()
            .ToList();

        // Uma única consulta ao banco
        var categorias = await _context.Categorias
            .AsNoTracking()
            .Where(c => categoryIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id);

        // Se alguma categoria não foi encontrada
        if (categorias.Count != categoryIds.Count)
        {
            return null;
        }

        var bebidas = _mapper.Map<List<Bebidas>>(dtos);

        foreach (var bebida in bebidas)
        {
            bebida.Name = bebida.Name.Trim();
        }

        await _context.Bebidas.AddRangeAsync(bebidas);
        await _context.SaveChangesAsync();

        var resultado = bebidas
            .Select(b => 
            {
                var dto = _mapper.Map<ReadBebidaDto>(b);

                dto.Category = categorias[b.CategoryId].Name;

                return dto;
            })
            .ToList();

        return resultado;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var bebida = await _context.Bebidas
            .FirstOrDefaultAsync(b => b.Id == id);
        if (bebida == null)
        {
            _logger.LogWarning(
            "Tentativa de excluir bebida inexistente. BebidaId: {BebidaId}",
            id);


            return false;
        }
        _context.Bebidas.Remove(bebida);
        await _context.SaveChangesAsync();
        _logger.LogInformation(
            "Bebida deletada. BebidaId: {BebidaId}, CategoryId: {CategoryId}",
            bebida.Id,
            bebida.CategoryId
        );
        return true;
    }
    public async Task<PagedResult<ReadBebidaDto>> GetPaginadoAsync(
    int pageNumber,
    int pageSize,
    string orderBy,
    string direction,
    string? name,
    int? categoryId,
    decimal? minPrice,
    decimal? maxPrice)
    {
        IQueryable<Bebidas> query = _context.Bebidas
            .AsNoTracking();

        // Filtros
        if (!string.IsNullOrWhiteSpace(name))
        {
            var nameFilter = name.Trim();

            query = query.Where(b =>
                b.Name.Contains(nameFilter));
        }

        if (categoryId.HasValue)
        {
            query = query.Where(b =>
                b.CategoryId == categoryId.Value);
        }

        if (minPrice.HasValue)
        {
            query = query.Where(b =>
                b.Price >= minPrice.Value);
        }

        if (maxPrice.HasValue)
        {
            query = query.Where(b =>
                b.Price <= maxPrice.Value);
        }

        // Conta depois dos filtros e antes da paginação
        var totalRegistros = await query.CountAsync();

        var totalPaginas = (int)Math.Ceiling(
            totalRegistros / (double)pageSize
        );

        // Ordenação
        query = orderBy switch
        {
            "name" => direction == "desc"
                ? query.OrderByDescending(b => b.Name)
                       .ThenByDescending(b => b.Id)
                : query.OrderBy(b => b.Name)
                       .ThenBy(b => b.Id),

            "price" => direction == "desc"
                ? query.OrderByDescending(b => b.Price)
                       .ThenByDescending(b => b.Id)
                : query.OrderBy(b => b.Price)
                       .ThenBy(b => b.Id),

            "quantity" => direction == "desc"
                ? query.OrderByDescending(b => b.Quantity)
                       .ThenByDescending(b => b.Id)
                : query.OrderBy(b => b.Quantity)
                       .ThenBy(b => b.Id),

            _ => direction == "desc"
                ? query.OrderByDescending(b => b.Id)
                : query.OrderBy(b => b.Id)
        };

        var bebidas = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ProjectTo<ReadBebidaDto>(_mapper.ConfigurationProvider)
            .ToListAsync();

        return new PagedResult<ReadBebidaDto>
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalRegistros = totalRegistros,
            TotalPaginas = totalPaginas,
            TemPaginaAnterior = pageNumber > 1,
            TemProximaPagina = pageNumber < totalPaginas,
            QuantidadeRetornada = bebidas.Count,
            OrderBy = orderBy,
            Direction = direction,
            Dados = bebidas
        };
    }




}