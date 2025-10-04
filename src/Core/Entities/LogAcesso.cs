namespace ControleDeAcesso.Core.Entities
{
    public class LogAcesso
    {
        public long Id { get; set; }
        public DateTime DataHora { get; set; }
        public string CartaoUid { get; set; } = string.Empty;
        public bool AcessoPermitido { get; set; }
        public string Motivo { get; set; } = string.Empty;
    }
}