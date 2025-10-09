using ControleDeAcesso.Core.Entities;

namespace ControleDeAcesso.Application.DTOs
{
    public class UsuarioDto
    {
        public string Id { get; set; } = string.Empty;
        public string Nome { get; set; } = string.Empty;
        public TipoUsuario Tipo { get; set; }
        public bool IsActive { get; set; }
        public List<CartaoDto> Cartoes { get; set; } = new List<CartaoDto>();
    }
}