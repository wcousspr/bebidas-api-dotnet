using crudEbancoDist8.DTOs;

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
}


