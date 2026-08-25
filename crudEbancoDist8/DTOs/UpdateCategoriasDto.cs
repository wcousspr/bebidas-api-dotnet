using System.ComponentModel.DataAnnotations;

namespace crudEbancoDist8.DTOs
{
    public class UpdateCategoriasDto
    {

        [Required(ErrorMessage = "O nome é obrigatório.")]
        [StringLength(100, MinimumLength = 2,
           ErrorMessage = "O nome deve ter entre 2 e 100 caracteres.")]

        public string Name { get; set; } = string.Empty;
    }
}
