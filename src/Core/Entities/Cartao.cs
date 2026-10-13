namespace ControleDeAcesso.Core.Entities
{
    public class Cartao
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Uid { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string UsuarioId { get; set; } = string.Empty;
        public Usuario Usuario { get; set; } = null!;
         public DateTime DataRegistro { get; set; } = DateTime.UtcNow;
    }
}