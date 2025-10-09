namespace ControleDeAcesso.Core.Entities
{
    public class Cartao
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Uid { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string PortadorId { get; set; } = string.Empty;
        public Portador Portador { get; set; } = null!;
    }
}