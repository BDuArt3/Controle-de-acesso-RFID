namespace ControleDeAcesso.Application.DTOs
{
    /// <summary>
    /// DTO de resposta para operações de gravação de cartão
    /// Não expõe o GUID completo por segurança
    /// </summary>
    public class GravacaoCartaoResponseDto
    {
        /// <summary>
        /// Identificador parcial do cartão (apenas para referência)
        /// </summary>
        public string CartaoRef { get; set; } = string.Empty;
        
        /// <summary>
        /// Status da solicitação de gravação
        /// </summary>
        public string Status { get; set; } = string.Empty;
        
        /// <summary>
        /// Mensagem descritiva do resultado
        /// </summary>
        public string Mensagem { get; set; } = string.Empty;
        
        /// <summary>
        /// ID do portador associado
        /// </summary>
        public string PortadorId { get; set; } = string.Empty;
        
        /// <summary>
        /// Nome do portador associado
        /// </summary>
        public string NomePortador { get; set; } = string.Empty;
        
        /// <summary>
        /// Timestamp da solicitação
        /// </summary>
        public DateTime DataSolicitacao { get; set; }
    }
}