using Microsoft.AspNetCore.SignalR;

namespace ControleDeAcesso.Api.Hubs;

/// <summary>
/// Hub SignalR responsável pela comunicação em tempo real 
/// para comandos de gravação de GUID em cartões RFID
/// </summary>
public class GravacaoHub : Hub
{
    private readonly ILogger<GravacaoHub> _logger;

    public GravacaoHub(ILogger<GravacaoHub> logger)
    {
        _logger = logger;
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