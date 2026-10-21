namespace ControleDeAcesso.Core.Entities
{
    public enum TipoMotivoAcesso
    {
        AcessoAutorizado = 1,
        UidObrigatorio = 2,
        CartaoNaoEncontrado = 3,
        CartaoDesativado = 4,
        UsuarioNaoEncontrado = 5,
        UsuarioDesativado = 6,
        ErroInterno = 7,
        CartaoExpirado = 8
    }

    public class LogAcesso
    {
        public long Id { get; set; }
        public DateTime DataHora { get; set; }
        public string? CartaoId { get; set; }
        public Cartao? Cartao { get; set; }
        public string? UsuarioId { get; set; }
        public Usuario? Usuario { get; set; }
        public string? UidInformado { get; set; }
        public bool AcessoPermitido { get; set; }
        public string Motivo { get; set; } = string.Empty;
        public TipoMotivoAcesso MotivoTipo { get; set; } = TipoMotivoAcesso.ErroInterno;
    }
}