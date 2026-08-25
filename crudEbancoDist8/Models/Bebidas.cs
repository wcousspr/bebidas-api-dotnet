namespace crudEbancoDist8.Models
{
    public class Bebidas
    {

        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; } = 0;

        public int Quantity { get; set; } = 0;

        public int CategoryId { get; set; }

        public Categoria Category { get; set; } = null!;
    }
}
