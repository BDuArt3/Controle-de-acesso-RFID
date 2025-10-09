namespace ControleDeAcesso.Core.Entities
{
    public enum TipoPortador
    {
        Aluno = 0,
        Professor = 1,
        Visitante = 2,
        Funcionario = 3
    }

    public class Portador
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Nome { get; set; } = string.Empty;
        public TipoPortador Tipo { get; set; }
        public bool IsActive { get; set; }
        public ICollection<Cartao> Cartoes { get; set; } = new List<Cartao>();
    }
}