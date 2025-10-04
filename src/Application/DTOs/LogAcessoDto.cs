namespace ControleDeAcesso.Application.DTOs
{
    public class LogAcessoDto
    {
        public long Id { get; set; }
        public DateTime DataHora { get; set; }
        public string CartaoUid { get; set; } = string.Empty;
        public bool AcessoPermitido { get; set; }
        public string Motivo { get; set; } = string.Empty;
        public string? UsuarioNome { get; set; }
        public string? UsuarioTipo { get; set; }
    }
}