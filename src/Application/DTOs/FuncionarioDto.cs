using ControleDeAcesso.Core.Entities;

namespace ControleDeAcesso.Application.DTOs
{
    public class FuncionarioDto
    {
        public string Id { get; set; } = string.Empty;
        public string NomeFuncionario { get; set; } = string.Empty;
        public string PortadorAcesso { get; set; } = string.Empty;
        public TipoFuncionario Tipo { get; set; }
        public bool IsActive { get; set; }
    }
}