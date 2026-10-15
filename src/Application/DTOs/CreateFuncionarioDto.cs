using ControleDeAcesso.Core.Entities;
using System.ComponentModel.DataAnnotations;

namespace ControleDeAcesso.Application.DTOs
{
    public class CreateFuncionarioDto
    {
        [Required]
        [StringLength(100)]
        public string NomeFuncionario { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(254)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Cpf { get; set; } = string.Empty;

        public string Senha { get; set; } = string.Empty;

        [EnumDataType(typeof(TipoFuncionario))]
        public TipoFuncionario Tipo { get; set; }
    }
}