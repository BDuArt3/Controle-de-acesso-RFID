using System.ComponentModel.DataAnnotations;

namespace ControleDeAcesso.Application.DTOs
{
    public class RefreshTokenDto
    {
        [Required(ErrorMessage = "Refresh token é obrigatório")]
        public string RefreshToken { get; set; } = string.Empty;
    }
}