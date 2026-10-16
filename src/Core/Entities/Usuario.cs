namespace ControleDeAcesso.Core.Entities
{
    public enum TipoUsuario
    {
        Aluno = 0,
        Professor = 1,
        Visitante = 2,
        Colaborador = 3
    }

    public class Usuario
    {
        private readonly List<Cartao> _cartoes = new();

        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Nome { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string Documento { get; set; } = string.Empty;
        public TipoUsuario Tipo { get; set; }
        public bool IsActive { get; set; }
        public IReadOnlyCollection<Cartao> Cartoes => _cartoes;
        public ICollection<LogAcesso> LogsAcesso { get; set; } = new List<LogAcesso>();
        public ICollection<HistoricoStatusUsuario> HistoricosStatus { get; set; } = new List<HistoricoStatusUsuario>();
        public Funcionario? Funcionario { get; set; }
    }
}