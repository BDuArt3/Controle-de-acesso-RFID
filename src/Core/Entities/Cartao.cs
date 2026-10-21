namespace ControleDeAcesso.Core.Entities
{
    public class Cartao
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Uid { get; init; } = string.Empty;
        public StatusCartao Status { get; private set; } = StatusCartao.Ativo;
        public string UsuarioId { get; init; } = string.Empty;
        public Usuario Usuario { get; private set; } = null!;
        public DateTime DataRegistro { get; set; } = DateTime.UtcNow;
        public DateTime? DataExpiracao { get; set; }
        public ICollection<LogAcesso> LogsAcesso { get; set; } = new List<LogAcesso>();
        public HistoricoAssociacoesCartao? HistoricoAssociacaoCartao { get; set; }
        public ICollection<HistoricoStatusCartao> HistoricosStatus { get; set; } = new List<HistoricoStatusCartao>();

        public bool AlterarStatus(StatusCartao novoStatus)
        {
            if (!Enum.IsDefined(novoStatus))
            {
                throw new ArgumentOutOfRangeException(nameof(novoStatus), novoStatus, "Status de cartão inválido.");
            }

            if (Status == novoStatus)
            {
                return false;
            }

            if (Status == StatusCartao.Cancelado)
            {
                throw new InvalidOperationException("Um cartão cancelado não pode mudar de status.");
            }

            Status = novoStatus;
            return true;
        }
    }
}