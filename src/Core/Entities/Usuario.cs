namespace ControleDeAcesso.Core.Entities
{
    public enum TipoUsuario
    {
        Aluno,
        Professor,
        Visitante
    }

    public class Usuario
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Nome { get; set; } = string.Empty;
        public TipoUsuario Tipo { get; set; }
        public bool IsActive { get; set; }
        public ICollection<Cartao> Cartoes { get; set; } = new List<Cartao>();
    }
}