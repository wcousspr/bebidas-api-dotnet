namespace crudEbancoDist8.DTOs
{
    public class BebidasQueryDto
    {
        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 10;

        public string OrderBy { get; set; } = "id";

        public string Direction { get; set; } = "asc";

        public string? Name { get; set; }

        public int? CategoryId { get; set; }

        public decimal? MinPrice { get; set; }

        public decimal? MaxPrice { get; set; }

        public int? MinQuantity { get; set; }

        public int? MaxQuantity { get; set; }
    }
}