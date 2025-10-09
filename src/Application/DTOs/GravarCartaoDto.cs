namespace ControleDeAcesso.Application.DTOs
{
    /// <summary>
    /// DTO para solicitação de gravação de novo cartão RFID
    /// O GUID é gerado automaticamente pelo sistema para segurança
    /// </summary>
    public class GravarCartaoDto
    {
        /// <summary>
        /// ID do usuário que receberá o cartão
        /// </summary>
        public string UsuarioId { get; set; } = string.Empty;
        
        /// <summary>
        /// Descrição opcional do cartão (ex: "Cartão principal", "Cartão backup", "Cartão temporário")
        /// Útil para diferenciar quando o usuário possui múltiplos cartões
        /// </summary>
        public string? Descricao { get; set; }
    }
}