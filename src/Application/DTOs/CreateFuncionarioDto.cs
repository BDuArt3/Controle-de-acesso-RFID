using ControleDeAcesso.Core.Entities;

namespace ControleDeAcesso.Application.DTOs
{
    public class CreateFuncionarioDto
    {
        public string NomeFuncionario { get; set; } = string.Empty;
        public string PortadorAcesso { get; set; } = string.Empty;
        public string Senha { get; set; } = string.Empty;
        public TipoFuncionario Tipo { get; set; }
    }
}