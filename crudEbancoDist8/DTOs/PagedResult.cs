namespace crudEbancoDist8.DTOs;

public class PagedResult<ReadBebidaDto>
{
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalRegistros { get; set; }
    public int TotalPaginas { get; set; }
    public bool TemPaginaAnterior { get; set; }
    public bool TemProximaPagina { get; set; }
    public int QuantidadeRetornada { get; set; }
    public string OrderBy { get; set; } = string.Empty;
    public string Direction { get; set; } = string.Empty;
    public List<ReadBebidaDto> Dados { get; set; } = [];
}