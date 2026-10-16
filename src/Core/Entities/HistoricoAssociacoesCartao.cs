namespace ControleDeAcesso.Core.Entities
{
    public class HistoricoAssociacoesCartao
    {
        public long Id { get; set; }
        public string CartaoId { get; set; } = string.Empty;
        public DateTime DataAssociacao { get; set; }
        public DateTime? DataDesassociacao { get; set; }
        public string? AssociadoPorFuncionarioId { get; set; }
        public Funcionario? AssociadoPorFuncionario { get; set; }
        public string? DesassociadoPorFuncionarioId { get; set; }
        public Funcionario? DesassociadoPorFuncionario { get; set; }
        public Cartao Cartao { get; set; } = null!;
    }
}
