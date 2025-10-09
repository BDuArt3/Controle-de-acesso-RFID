using ControleDeAcesso.Core.Entities;

namespace ControleDeAcesso.Application.DTOs
{
    public class CreatePortadorDto
    {
        public string Nome { get; set; } = string.Empty;
        public TipoPortador Tipo { get; set; }
    }
}