namespace ControleDeAcesso.Core.Entities
{
    public class HistoricoStatusFuncionario
    {
        public long Id { get; set; }
        public string FuncionarioId { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime DataHora { get; set; }
        public string? Motivo { get; set; }
        public string? AlteradoPorFuncionarioId { get; set; }
        public Funcionario Funcionario { get; set; } = null!;
        public Funcionario? AlteradoPorFuncionario { get; set; }
    }
}
