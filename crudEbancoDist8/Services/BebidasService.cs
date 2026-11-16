using crudEbancoDist.Data;
using crudEbancoDist8.DTOs;
using Microsoft.EntityFrameworkCore;
using crudEbancoDist8.Models;
using crudEbancoDist8.Controllers;
using crudEbancoDist8.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using crudEbancoDist8.Interface;

namespace crudEbancoDist8.Services;

public class BebidasService : IBebidasService
{

    private readonly AppDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<BebidasService> _logger;
    private readonly IImageStorageService _imageStorageService;

    public BebidasService(
        AppDbContext context,
        IMapper mapper,
        ILogger<BebidasService> logger,
        IImageStorageService imageStorageService)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
        _imageStorageService = imageStorageService;
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



    public async Task<StoredImageResult?> UploadImageAsync(
        int bebidaId,
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        var bebida = await _context.Bebidas
            .FirstOrDefaultAsync(
                b => b.Id == bebidaId,
                cancellationToken);

        if (bebida == null)
        {
            _logger.LogWarning(
                "Tentativa de enviar imagem para bebida inexistente. BebidaId: {BebidaId}",
                bebidaId);

            return null;
        }

        // Guarda o identificador da imagem anterior.
        var imagemAntigaPublicId = bebida.ImagePublicId;

        // Envia a nova imagem ao Cloudinary.
        var novaImagem = await _imageStorageService.UploadAsync(
            file,
            cancellationToken);

        try
        {
            // Atualiza os dados da bebida.
            bebida.ImageUrl = novaImagem.Url;
            bebida.ImagePublicId = novaImagem.PublicId;

            // Confirma a alteração no banco.
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            // Se o banco falhar, tenta remover a nova imagem.
            try
            {
                await _imageStorageService.DeleteAsync(
                    novaImagem.PublicId,
                    CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Falha ao limpar imagem após erro no banco. PublicId: {PublicId}",
                    novaImagem.PublicId);
            }

            throw;
        }

        // Somente após salvar no banco, remove a imagem antiga.
        if (!string.IsNullOrWhiteSpace(imagemAntigaPublicId) &&
            imagemAntigaPublicId != novaImagem.PublicId)
        {
            try
            {
                await _imageStorageService.DeleteAsync(
                    imagemAntigaPublicId,
                    CancellationToken.None);
            }
            catch (Exception ex)
            {
                // Não desfaz a gravação: a nova imagem já está salva.
                _logger.LogWarning(
                    ex,
                    "Imagem atualizada, mas não foi possível excluir a imagem anterior. PublicId: {PublicId}",
                    imagemAntigaPublicId);
            }
        }

        _logger.LogInformation(
            "Imagem atualizada para bebida. BebidaId: {BebidaId}",
            bebidaId);

        return novaImagem;
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
    BebidasQueryDto filtros)
    {
        IQueryable<Bebidas> query = _context.Bebidas
            .AsNoTracking();

        // Filtros
        if (!string.IsNullOrWhiteSpace(filtros.Name))
        {
            var nameFilter = filtros.Name.Trim();

            query = query.Where(b =>
                b.Name.Contains(nameFilter));
        }

        if (filtros.CategoryId.HasValue)
        {
            query = query.Where(b =>
                b.CategoryId == filtros.CategoryId.Value);
        }

        if (filtros.MinPrice.HasValue)
        {
            query = query.Where(b =>
                b.Price >= filtros.MinPrice.Value);
        }

        if (filtros.MaxPrice.HasValue)
        {
            query = query.Where(b =>
                b.Price <= filtros.MaxPrice.Value);
        }

        if (filtros.MinQuantity.HasValue)
        {
            query = query.Where(b =>
                b.Quantity >= filtros.MinQuantity.Value);
        }

        if (filtros.MaxQuantity.HasValue)
        {
            query = query.Where(b =>
                b.Quantity <= filtros.MaxQuantity.Value);
        }

        

        // Conta depois dos filtros e antes da paginação
        var totalRegistros = await query.CountAsync();

        var totalPaginas = (int)Math.Ceiling(
            totalRegistros / (double)filtros.PageSize   
        );

        // Ordenação
        query = filtros.OrderBy switch
        {
            "name" => filtros.Direction == "desc"
                ? query.OrderByDescending(b => b.Name)
                       .ThenByDescending(b => b.Id)
                : query.OrderBy(b => b.Name)
                       .ThenBy(b => b.Id),

            "price" => filtros.Direction == "desc"
                ? query.OrderByDescending(b => b.Price)
                       .ThenByDescending(b => b.Id)
                : query.OrderBy(b => b.Price)
                       .ThenBy(b => b.Id),

            "quantity" => filtros.Direction == "desc"
                ? query.OrderByDescending(b => b.Quantity)
                       .ThenByDescending(b => b.Id)
                : query.OrderBy(b => b.Quantity)
                       .ThenBy(b => b.Id),

            _ => filtros.Direction == "desc"
                ? query.OrderByDescending(b => b.Id)
                : query.OrderBy(b => b.Id)
        };

        var bebidas = await query
            .Skip((filtros.PageNumber - 1) * filtros.PageSize)
            .Take(filtros.PageSize)
            .ProjectTo<ReadBebidaDto>(_mapper.ConfigurationProvider)
            .ToListAsync();

        return new PagedResult<ReadBebidaDto>
        {
            PageNumber = filtros.PageNumber,
            PageSize = filtros.PageSize,
            TotalRegistros = totalRegistros,
            TotalPaginas = totalPaginas,
            TemPaginaAnterior = filtros.PageNumber > 1,
            TemProximaPagina = filtros.PageNumber < totalPaginas,
            QuantidadeRetornada = bebidas.Count,
            OrderBy = filtros.OrderBy,
            Direction = filtros.Direction,
            Dados = bebidas,
            
        };
    }




}