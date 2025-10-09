namespace ControleDeAcesso.Application.DTOs
{
    public class AccessCheckResponseDto
    {
        public bool Allowed { get; set; }
        public string Message { get; set; } = string.Empty;
        public long? PortadorId { get; set; }
        public string? NomePortador { get; set; }
        public string? PortadorNome { get; set; }
        public string? PortadorTipo { get; set; }
    }
}