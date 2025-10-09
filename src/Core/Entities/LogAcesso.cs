namespace ControleDeAcesso.Core.Entities
{
    public enum TipoMotivoAcesso
    {
        AcessoAutorizado = 1,
        UidObrigatorio = 2,
        CartaoNaoEncontrado = 3,
        CartaoDesativado = 4,
        PortadorNaoEncontrado = 5,
        PortadorDesativado = 6,
        ErroInterno = 7
    }

    public class LogAcesso
    {
        public long Id { get; set; }
        public DateTime DataHora { get; set; }
        public string CartaoUid { get; set; } = string.Empty;
        public bool AcessoPermitido { get; set; }
        public string Motivo { get; set; } = string.Empty;
        public TipoMotivoAcesso MotivoTipo { get; set; } = TipoMotivoAcesso.ErroInterno;
    }
}