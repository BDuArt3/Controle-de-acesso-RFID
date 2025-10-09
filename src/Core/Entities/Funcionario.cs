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
        public string NomeFuncionario { get; set; } = string.Empty;
        public string UsuarioAcesso { get; set; } = string.Empty;
        public string SenhaHash { get; set; } = string.Empty;
        public TipoFuncionario Tipo { get; set; }
        public bool IsActive { get; set; }
    }
}