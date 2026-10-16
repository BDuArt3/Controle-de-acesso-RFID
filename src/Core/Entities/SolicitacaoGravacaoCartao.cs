namespace ControleDeAcesso.Core.Entities
{
    public enum StatusSolicitacaoGravacao
    {
        Pendente = 0,
        Enviada = 1,
        Concluida = 2,
        Falhou = 3,
        Cancelada = 4
    }

    public class SolicitacaoGravacaoCartao
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string ChaveIdempotencia { get; set; } = string.Empty;
        public string Credencial { get; set; } = string.Empty;
        public string UsuarioId { get; set; } = string.Empty;
        public string SolicitanteFuncionarioId { get; set; } = string.Empty;
        public string? CartaoAnteriorId { get; set; }
        public string? CartaoCriadoId { get; set; }
        public StatusSolicitacaoGravacao Status { get; private set; } = StatusSolicitacaoGravacao.Pendente;
        public DateTime DataSolicitacao { get; set; } = DateTime.UtcNow;
        public DateTime? DataUltimoEnvio { get; private set; }
        public DateTime? DataConclusao { get; private set; }
        public DateTime? DataCancelamento { get; private set; }
        public int QuantidadeEnvios { get; private set; }
        public string? MotivoReemissao { get; set; }
        public string? MotivoFalha { get; private set; }
        public string? CanceladoPorFuncionarioId { get; private set; }

        public bool MarcarEnviada(DateTime dataHora)
        {
            if (Status != StatusSolicitacaoGravacao.Pendente)
            {
                return false;
            }

            Status = StatusSolicitacaoGravacao.Enviada;
            DataUltimoEnvio = dataHora;
            QuantidadeEnvios++;
            MotivoFalha = null;
            return true;
        }

        public bool PrepararRetentativa(DateTime dataHora, TimeSpan idadeMinimaEnvio)
        {
            if (Status == StatusSolicitacaoGravacao.Pendente && QuantidadeEnvios == 0)
            {
                return true;
            }

            var falhaRegistrada = Status == StatusSolicitacaoGravacao.Falhou;
            var envioExpirado = Status == StatusSolicitacaoGravacao.Enviada &&
                DataUltimoEnvio.HasValue &&
                dataHora - DataUltimoEnvio.Value >= idadeMinimaEnvio;
            if (!falhaRegistrada && !envioExpirado)
            {
                return false;
            }

            Status = StatusSolicitacaoGravacao.Pendente;
            MotivoFalha = null;
            return true;
        }

        public bool MarcarFalha(string motivo)
        {
            if (Status is StatusSolicitacaoGravacao.Concluida or StatusSolicitacaoGravacao.Cancelada)
            {
                return false;
            }

            Status = StatusSolicitacaoGravacao.Falhou;
            MotivoFalha = motivo;
            return true;
        }

        public bool MarcarConcluida(string cartaoId, DateTime dataHora)
        {
            if (Status is StatusSolicitacaoGravacao.Concluida or StatusSolicitacaoGravacao.Cancelada)
            {
                return false;
            }

            Status = StatusSolicitacaoGravacao.Concluida;
            CartaoCriadoId = cartaoId;
            DataConclusao = dataHora;
            MotivoFalha = null;
            return true;
        }

        public bool Cancelar(string motivo, string funcionarioId, DateTime dataHora)
        {
            if (Status is StatusSolicitacaoGravacao.Concluida or StatusSolicitacaoGravacao.Cancelada)
            {
                return false;
            }

            Status = StatusSolicitacaoGravacao.Cancelada;
            MotivoFalha = motivo;
            CanceladoPorFuncionarioId = funcionarioId;
            DataCancelamento = dataHora;
            return true;
        }
    }
}
