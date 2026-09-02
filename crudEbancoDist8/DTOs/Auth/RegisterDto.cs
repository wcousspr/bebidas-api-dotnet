using System.ComponentModel.DataAnnotations;

namespace crudEbancoDist8.DTOs.Auth
    
{
    public class RegisterDto
    {
        [Required(ErrorMessage = "O nome de usuário é obrigatório.")]
        [MinLength(3, ErrorMessage = "O nome deve possuir pelo menos 3 caracteres.")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "O e-mail é obrigatório.")]
        [EmailAddress(ErrorMessage = "Informe um e-mail válido.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "A senha é obrigatória.")]
        [MinLength(6, ErrorMessage = "A senha deve possuir pelo menos 6 caracteres.")]
        public string Password { get; set; } = string.Empty;

    }
}
