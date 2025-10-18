namespace ControleDeAcesso.Application.DTOs
{
    public class CartaoDto
    {
        public string Id { get; set; } = string.Empty;
        public string Uid { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string UsuarioId { get; set; } = string.Empty;
        public string NomeUsuario { get; set; } = string.Empty;
    }
}