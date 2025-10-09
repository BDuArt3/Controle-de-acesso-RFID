namespace ControleDeAcesso.Application.DTOs
{
    /// <summary>
    /// DTO para solicitação de gravação de novo cartão RFID
    /// O GUID é gerado automaticamente pelo sistema para segurança
    /// </summary>
    public class GravarCartaoDto
    {
        /// <summary>
        /// ID do portador que receberá o cartão
        /// </summary>
        public string PortadorId { get; set; } = string.Empty;
        
        /// <summary>
        /// Descrição opcional do cartão (ex: "Cartão principal", "Cartão backup", "Cartão temporário")
        /// Útil para diferenciar quando o portador possui múltiplos cartões
        /// </summary>
        public string? Descricao { get; set; }
    }
}