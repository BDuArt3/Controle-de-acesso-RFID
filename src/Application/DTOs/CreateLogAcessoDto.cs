using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using ControleDeAcesso.Core.Entities;

namespace ControleDeAcesso.Application.DTOs
{
    public class CreateLogAcessoDto
    {
        [StringLength(50, ErrorMessage = "UID deve ter no máximo 50 caracteres")]
        public string? CartaoUid { get; set; }

        [JsonIgnore]
        public string? CartaoId { get; set; }

        [JsonIgnore]
        public string? UsuarioId { get; set; }

        [JsonIgnore]
        public string? UidInformado { get; set; }

        [Required(ErrorMessage = "Campo AcessoPermitido é obrigatório")]
        public bool AcessoPermitido { get; set; }

        [StringLength(200, ErrorMessage = "Motivo deve ter no máximo 200 caracteres")]
        public string Motivo { get; set; } = string.Empty;

        [Required(ErrorMessage = "Tipo do motivo é obrigatório")]
        public TipoMotivoAcesso MotivoTipo { get; set; } = TipoMotivoAcesso.ErroInterno;
    }
}