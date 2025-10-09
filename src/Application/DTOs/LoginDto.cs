using System.ComponentModel.DataAnnotations;

namespace ControleDeAcesso.Application.DTOs
{
    public class LoginDto
    {
        [Required(ErrorMessage = "Portador de acesso é obrigatório")]
        public string PortadorAcesso { get; set; } = string.Empty;

        [Required(ErrorMessage = "Senha é obrigatória")]
        public string Senha { get; set; } = string.Empty;
    }
}