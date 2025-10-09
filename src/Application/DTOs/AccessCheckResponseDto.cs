namespace ControleDeAcesso.Application.DTOs
{
    public class AccessCheckResponseDto
    {
        public bool Allowed { get; set; }
        public string Message { get; set; } = string.Empty;
        public long? UsuarioId { get; set; }
        public string? NomeUsuario { get; set; }
        public string? UsuarioNome { get; set; }
        public string? UsuarioTipo { get; set; }
    }
}