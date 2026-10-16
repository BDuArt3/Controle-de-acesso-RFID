namespace ControleDeAcesso.Core.Entities
{
    public enum TipoFuncionario
    {
        Visualizador = 0,  // Apenas consulta - menor privilégio
        Operador = 1,      // Pode cadastrar usuários e gravar cartões
        Admin = 2          // Acesso completo - maior privilégio
    }

    public class Funcionario
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string UsuarioId { get; set; } = string.Empty;
        public Usuario Usuario { get; set; } = null!;
        public string SenhaHash { get; set; } = string.Empty;
        public string Departamento { get; set; } = string.Empty;
        public string Cargo { get; set; } = string.Empty;
        public TipoFuncionario Tipo { get; set; }
        public bool IsActive { get; set; }
        public ICollection<HistoricoAssociacoesCartao> HistoricosAssociacaoRealizadas { get; set; } = new List<HistoricoAssociacoesCartao>();
        public ICollection<HistoricoAssociacoesCartao> HistoricosAssociacaoEncerradas { get; set; } = new List<HistoricoAssociacoesCartao>();
        public ICollection<HistoricoStatusCartao> HistoricosStatusCartaoAlterados { get; set; } = new List<HistoricoStatusCartao>();
        public ICollection<HistoricoStatusUsuario> HistoricosStatusUsuarioAlterados { get; set; } = new List<HistoricoStatusUsuario>();
        public ICollection<HistoricoStatusFuncionario> HistoricosStatusFuncionario { get; set; } = new List<HistoricoStatusFuncionario>();
        public ICollection<HistoricoStatusFuncionario> HistoricosStatusFuncionarioAlterados { get; set; } = new List<HistoricoStatusFuncionario>();
    }
}