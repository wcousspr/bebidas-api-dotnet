using System.ComponentModel.DataAnnotations;


namespace crudEbancoDist8.DTOs
{
    public class CreateBebidaDto
    {

        [Required(ErrorMessage = "O nome é obrigatório.")]
        [StringLength(100, MinimumLength = 2,
        ErrorMessage = "O nome deve ter entre 2 e 100 caracteres.")]
        public string Name { get; set; } = string.Empty;

        [Range(0.01, 10000,
            ErrorMessage = "O preço deve ser maior que zero.")]
        public decimal Price { get; set; }

        [Range(0, 100000,
            ErrorMessage = "A quantidade não pode ser negativa.")]
        public int Quantity { get; set; }

        [Required(ErrorMessage = "A categoria é obrigatória.")]
        public int CategoryId { get; set; } = 0;


    }
}
