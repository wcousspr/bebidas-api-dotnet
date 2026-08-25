namespace crudEbancoDist8.DTOs
{
    public class ReadBebidaDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public decimal Price { get; set; }

        public int Quantity { get; set; }

        public string Category { get; set; } = string.Empty;

    }
}
