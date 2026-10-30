using ControleDeAcesso.Core.Entities;

namespace ControleDeAcesso.Application.DTOs
{
    public class LogAcessoDto
    {
        public long Id { get; set; }
        public DateTime DataHora { get; set; }
        public string Local { get; set; } = LogAcesso.LocalPadrao;
        public string? CartaoId { get; set; }
        public string? UsuarioId { get; set; }
        public string? UidInformado { get; set; }
        public string CartaoUid { get; set; } = string.Empty;
        public bool AcessoPermitido { get; set; }
        public string Motivo { get; set; } = string.Empty;
        public TipoMotivoAcesso MotivoTipo { get; set; }
        public string? UsuarioNome { get; set; }
        public string? UsuarioTipo { get; set; }
    }
}