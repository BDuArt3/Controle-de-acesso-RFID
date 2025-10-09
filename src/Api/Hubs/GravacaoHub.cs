using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace ControleDeAcesso.Api.Hubs;

/// <summary>
/// Hub SignalR responsável pela comunicação em tempo real 
/// para comandos de gravação de GUID em cartões RFID
/// </summary>
public class GravacaoHub : Hub
{
    private readonly ILogger<GravacaoHub> _logger;
    
    // Dicionário para rastrear solicitações de gravação pendentes: GUID -> PortadorId
    private static readonly ConcurrentDictionary<string, string> _solicitacoesPendentes = new();

    public GravacaoHub(ILogger<GravacaoHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Adiciona uma solicitação de gravação pendente
    /// </summary>
    public static void AdicionarSolicitacaoPendente(string guid, string portadorId)
    {
        var guidReduzido = guid.Replace("-", "").Substring(0, 16);
        _solicitacoesPendentes.TryAdd(guidReduzido, portadorId);
    }

    /// <summary>
    /// Remove e retorna o portador ID de uma solicitação pendente
    /// </summary>
    public static string? RemoverSolicitacaoPendente(string guidGravado)
    {
        _solicitacoesPendentes.TryRemove(guidGravado, out var portadorId);
        return portadorId;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Cliente conectado ao GravacaoHub: {connectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Cliente desconectado do GravacaoHub: {connectionId}", Context.ConnectionId);
        if (exception != null)
        {
            _logger.LogError(exception, "Erro na desconexão do cliente {connectionId}", Context.ConnectionId);
        }
        await base.OnDisconnectedAsync(exception);
    }
}