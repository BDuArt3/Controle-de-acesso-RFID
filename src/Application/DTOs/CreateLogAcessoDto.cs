using System.ComponentModel.DataAnnotations;
using ControleDeAcesso.Core.Entities;

namespace ControleDeAcesso.Application.DTOs
{
    public class CreateLogAcessoDto
    {
        [Required(ErrorMessage = "UID do cartão é obrigatório")]
        [StringLength(50, ErrorMessage = "UID deve ter no máximo 50 caracteres")]
        public string CartaoUid { get; set; } = string.Empty;

        [Required(ErrorMessage = "Campo AcessoPermitido é obrigatório")]
        public bool AcessoPermitido { get; set; }

        [StringLength(200, ErrorMessage = "Motivo deve ter no máximo 200 caracteres")]
        public string Motivo { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tipo do motivo é obrigatório")]
        public TipoMotivoAcesso MotivoTipo { get; set; } = TipoMotivoAcesso.ErroInterno;
    }
}