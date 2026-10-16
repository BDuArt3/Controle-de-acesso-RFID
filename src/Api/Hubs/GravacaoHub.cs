using ControleDeAcesso.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ControleDeAcesso.Api.Hubs;

[Authorize(Policy = "WorkerCallback")]
public class GravacaoHub : Hub
{
    private readonly ILogger<GravacaoHub> _logger;
    private static int _connectedWorkers;

    public GravacaoHub(ILogger<GravacaoHub> logger)
    {
        _logger = logger;
    }

    public static bool HasConnectedWorker => Volatile.Read(ref _connectedWorkers) > 0;

    public override async Task OnConnectedAsync()
    {
        Interlocked.Increment(ref _connectedWorkers);
        _logger.LogInformation("Worker conectado ao GravacaoHub: {connectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        Interlocked.Decrement(ref _connectedWorkers);
        _logger.LogInformation("Worker desconectado do GravacaoHub: {connectionId}", Context.ConnectionId);
        if (exception != null)
        {
            _logger.LogError(exception, "Erro na desconexão do Worker {connectionId}", Context.ConnectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }
}
