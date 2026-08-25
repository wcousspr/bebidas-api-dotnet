namespace crudEbancoDist8.Models
{
    public class Categoria
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        public List<Bebidas> Bebidas { get; set; } = new();
    }
}
