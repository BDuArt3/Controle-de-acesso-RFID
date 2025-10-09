using ControleDeAcesso.Core.Entities;

namespace ControleDeAcesso.Application.DTOs
{
    public class CreateUsuarioDto
    {
        public string Nome { get; set; } = string.Empty;
        public TipoUsuario Tipo { get; set; }
    }
}