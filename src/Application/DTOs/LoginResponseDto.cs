namespace ControleDeAcesso.Application.DTOs
{
    public class LoginResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public TokenDto? Token { get; set; }
        public FuncionarioDto? Usuario { get; set; }
    }
}