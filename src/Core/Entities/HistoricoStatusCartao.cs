namespace ControleDeAcesso.Core.Entities
{
    public class HistoricoStatusCartao
    {
        public long Id { get; set; }
        public string CartaoId { get; set; } = string.Empty;
        public StatusCartao Status { get; set; }
        public DateTime DataHora { get; set; }
        public string? Motivo { get; set; }
        public string? AlteradoPorFuncionarioId { get; set; }
        public Funcionario? AlteradoPorFuncionario { get; set; }
        public Cartao Cartao { get; set; } = null!;
    }
}
