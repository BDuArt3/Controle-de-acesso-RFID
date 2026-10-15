namespace ControleDeAcesso.Application.DTOs
{
    /// <summary>Estado de uma operação de gravação de cartão RFID.</summary>
    public sealed record GravacaoCartaoResponseDto(
        string SolicitacaoId,
        string Status,
        string Mensagem,
        string UsuarioId,
        string NomeUsuario,
        DateTimeOffset DataSolicitacao,
        string? CartaoId);

    /// <summary>Resultado interno que mantém a credencial fora da resposta HTTP.</summary>
    public sealed record GravacaoSolicitadaResult(
        GravacaoCartaoResponseDto Response,
        GravacaoCartaoCommandDto? Command);

    /// <summary>Comando privado enviado pelo SignalR ao Worker RFID.</summary>
    public sealed record GravacaoCartaoCommandDto(
        string SolicitacaoId,
        string Credencial);
}
