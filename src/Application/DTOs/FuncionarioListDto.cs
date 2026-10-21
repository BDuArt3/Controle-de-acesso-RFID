using ControleDeAcesso.Core.Entities;

namespace ControleDeAcesso.Application.DTOs
{
    public class FuncionarioListDto
    {
        public string Id { get; set; } = string.Empty;
        public string NomeFuncionario { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public TipoFuncionario Tipo { get; set; }
        public bool IsActive { get; set; }
    }
}
