using ControleDeAcesso.Core.Entities;
using System.ComponentModel.DataAnnotations;

namespace ControleDeAcesso.Application.DTOs
{
    public class CreateUsuarioDto
    {
        [Required]
        [StringLength(100)]
        public string Nome { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email é obrigatório.")]
        [EmailAddress]
        [StringLength(254)]
        public string? Email { get; set; }

        public string Documento { get; set; } = string.Empty;

        [EnumDataType(typeof(TipoUsuario))]
        public TipoUsuario Tipo { get; set; }
    }
}