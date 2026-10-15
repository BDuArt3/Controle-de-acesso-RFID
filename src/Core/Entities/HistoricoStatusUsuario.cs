namespace ControleDeAcesso.Core.Entities
{
    public class HistoricoStatusUsuario
    {
        public long Id { get; set; }
        public string UsuarioId { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime DataHora { get; set; }
        public string? Motivo { get; set; }
        public string? AlteradoPorFuncionarioId { get; set; }
        public Funcionario? AlteradoPorFuncionario { get; set; }
        public Usuario Usuario { get; set; } = null!;
    }
}
