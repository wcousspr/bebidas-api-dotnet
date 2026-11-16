using crudEbancoDist8.DTOs;
using crudEbancoDist8.Interface;
using Microsoft.AspNetCore.Http;




namespace crudEbancoDist8.Interfaces;

public interface IBebidasService
{
    Task<List<ReadBebidaDto>> GetAllAsync();

    Task<ReadBebidaDto?> GetByIdAsync(int id);

    Task<List<ReadBebidaDto>?> CreateBebidasInLote(
    List<CreateBebidaDto> dtos);

    Task<ReadBebidaDto?> CreateAsync(
        CreateBebidaDto createBebidaDto);

    Task<(ReadBebidaDto? Bebida, string? Erro)> UpdateAsync(
        int id,
        UpdateBebidaDto updateBebidaDto);

    Task<bool> DeleteAsync(int id);

    Task<PagedResult<ReadBebidaDto>> GetPaginadoAsync(
    BebidasQueryDto filtros);

    Task<StoredImageResult?> UploadImageAsync(
    int bebidaId,
    IFormFile file,
    CancellationToken cancellationToken = default);
}


