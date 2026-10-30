namespace ControleDeAcesso.Worker;

using System.IO.Ports;
using ControleDeAcesso.Application.DTOs;
using ControleDeAcesso.Core.Entities;
using Microsoft.AspNetCore.SignalR.Client;
public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly WorkerApiClient _apiClient;
    private readonly string _apiBaseUrl;
    private readonly string _comPortName;
    private readonly string _workerApiKey;
    private readonly string _localAcesso;
    private SerialPort? _serialPort;
    private readonly object _serialWriteLock = new();
    private HubConnection? _hubConnection;
    private bool _isDisposing = false;
    private readonly SemaphoreSlim _recordingLock = new(1, 1);
    
    // Variáveis para controle de leitura de acesso
    private string? _ultimoUidLido = null;
    private string? _ultimoGuidLido = null;
    
    private string? _solicitacaoSendoGravada;
    private string? _credencialSendoGravada;
    private bool _gravacaoRecenteRealizada = false;

    private readonly MedicaoTempoResposta _medicao;
    private readonly object _leituraLock = new();
    private DateTime? _inicioLeituraUtc;
    private long _inicioLeituraTicks;

    public Worker(
        ILogger<Worker> logger,
        IConfiguration configuration,
        MedicaoTempoResposta medicao
        )
    {
        _logger = logger;
        _medicao = medicao;
        var configuredApiBaseUrl = configuration["ApiSettings:BaseUrl"];
        if (!Uri.TryCreate(configuredApiBaseUrl, UriKind.Absolute, out var apiBaseUri) ||
            (apiBaseUri.Scheme != Uri.UriSchemeHttp && apiBaseUri.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException(
                "ApiSettings:BaseUrl deve conter uma URL absoluta HTTP ou HTTPS.");
        }

        _apiBaseUrl = apiBaseUri.ToString().TrimEnd('/');
        _comPortName = configuration["SerialPortSettings:ComPortName"] ?? "COM4";
        var localConfigurado = configuration["AccessPoint:Location"];
        _localAcesso = string.IsNullOrWhiteSpace(localConfigurado)
            ? LogAcesso.LocalPadrao
            : localConfigurado.Trim();
        if (_localAcesso.Length > 100)
        {
            throw new InvalidOperationException("AccessPoint:Location deve ter no máximo 100 caracteres.");
        }

        _workerApiKey = configuration["WorkerAuth:ApiKey"]
            ?? throw new InvalidOperationException("WorkerAuth:ApiKey não configurada para o Worker.");
        if (_workerApiKey.Length < 32)
        {
            throw new InvalidOperationException("WorkerAuth:ApiKey deve ter pelo menos 32 caracteres.");
        }

        _apiClient = new WorkerApiClient(new HttpClient(), _apiBaseUrl, _workerApiKey);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker service iniciando...");

        while (!stoppingToken.IsCancellationRequested)
        {
            if (_hubConnection == null || _hubConnection.State == HubConnectionState.Disconnected)
            {
                await ConfigurarSignalRConnection(stoppingToken);
            }

            // Tentar configurar porta serial se não estiver conectada
            if (_serialPort?.IsOpen != true)
            {
                TentarConfigurarPortaSerial();
            }
            
            await Task.Delay(5000, stoppingToken); // Verificar a cada 5 segundos
        }
    }

    private void TentarConfigurarPortaSerial()
    {
        try
        {
            _serialPort = new SerialPort(_comPortName, 9600)
            {
                ReadTimeout = 500,
                WriteTimeout = 500
            };
            _serialPort.DataReceived += SerialPort_DataReceived;
            _serialPort.Open();

            _logger.LogInformation("Porta serial {portName} aberta com sucesso.", _comPortName);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Acesso negado à porta serial '{portName}'. Verifique se outra aplicação está usando a porta ou se o Arduino está conectado corretamente. O Worker continuará funcionando apenas com SignalR. Erro: {message}", _comPortName, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Não foi possível abrir a porta serial '{portName}'. O Worker continuará funcionando apenas com SignalR.", _comPortName);
        }
    }
    private async Task ConfigurarSignalRConnection(CancellationToken stoppingToken)
    {
        try
        {
            if (_hubConnection != null)
            {
                await _hubConnection.DisposeAsync();
            }

            _hubConnection = new HubConnectionBuilder()
                .WithUrl($"{_apiBaseUrl}/gravacaoHub", options =>
                {
                    options.Headers.Add("X-Worker-Key", _workerApiKey);
                })
                .WithAutomaticReconnect()
                .Build();

            // Handler para receber comandos de gravação
            _hubConnection.On<GravacaoCartaoCommandDto>("ReceberComandoGravacao", async (command) =>
            {
                _logger.LogInformation(
                    "Comando de gravação recebido para solicitação {SolicitacaoId}.",
                    command.SolicitacaoId);
                await ProcessarComandoGravacao(command);
            });

            await _hubConnection.StartAsync(stoppingToken);
            _logger.LogInformation("Conexão SignalR estabelecida com sucesso em: {url}", $"{_apiBaseUrl}/gravacaoHub");
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao estabelecer conexão SignalR com o Hub.");
            if (_hubConnection != null)
            {
                await _hubConnection.DisposeAsync();
                _hubConnection = null;
            }
        }
    }
    private async Task ProcessarComandoGravacao(GravacaoCartaoCommandDto command)
    {
        await _recordingLock.WaitAsync();
        try
        {
            if (_serialPort?.IsOpen != true)
            {
                await ReportarFalhaGravacaoAsync(command.SolicitacaoId, "Porta serial não está disponível.");
                return;
            }

            if (command.Credencial.Length != 16 ||
                command.Credencial.Any(character => !Uri.IsHexDigit(character)))
            {
                await ReportarFalhaGravacaoAsync(command.SolicitacaoId, "Credencial recebida possui formato inválido.");
                return;
            }

            _solicitacaoSendoGravada = command.SolicitacaoId;
            _credencialSendoGravada = command.Credencial;
            lock (_serialWriteLock)
            {
                _serialPort.WriteLine($"WRITE:{command.Credencial}");
            }
            _logger.LogInformation(
                "Comando enviado ao Arduino para solicitação {SolicitacaoId}.",
                command.SolicitacaoId);

            await Task.Delay(TimeSpan.FromSeconds(17));
            if (_solicitacaoSendoGravada == command.SolicitacaoId)
            {
                _solicitacaoSendoGravada = null;
                _credencialSendoGravada = null;
                await ReportarFalhaGravacaoAsync(
                    command.SolicitacaoId,
                    "Tempo limite excedido aguardando confirmação do Arduino.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Erro ao processar comando da solicitação {SolicitacaoId}.",
                command.SolicitacaoId);
            _solicitacaoSendoGravada = null;
            _credencialSendoGravada = null;
            await ReportarFalhaGravacaoAsync(command.SolicitacaoId, "Erro ao processar o comando no Worker.");
        }
        finally
        {
            _recordingLock.Release();
        }
    }

    private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        if (_serialPort == null || !_serialPort.IsOpen || _isDisposing) return;

        try
        {
            string data = _serialPort.ReadLine().Trim();

            if (!string.IsNullOrEmpty(data))
            {
                // Verificar se é uma resposta de gravação do Arduino
                if (data.StartsWith("SUCCESS:"))
                {
                    _logger.LogInformation(">>> Gravação bem-sucedida confirmada pelo Arduino: {resposta} <<<", data);
                    
                    // Marcar que uma gravação recente foi realizada
                    _gravacaoRecenteRealizada = true;
                    
                    // Se há um GUID sendo gravado, confirmar a gravação
                    if (!string.IsNullOrEmpty(_solicitacaoSendoGravada) &&
                        !string.IsNullOrEmpty(_credencialSendoGravada))
                    {
                        var solicitacaoId = _solicitacaoSendoGravada;
                        var credencial = _credencialSendoGravada;
                        _solicitacaoSendoGravada = null;
                        _credencialSendoGravada = null;
                        _ = Task.Run(async () =>
                        {
                            await ConfirmarGravacaoCartao(solicitacaoId, credencial);
                            // Aguardar um pouco para garantir que a gravação foi processada na API
                            await Task.Delay(1000);
                            _gravacaoRecenteRealizada = false;
                            _logger.LogInformation(">>> Gravação confirmada na API - pronto para verificação de acesso <<<");
                        });
                    }
                }
                else if (data.StartsWith("ERROR:"))
                {
                    _logger.LogError(">>> Erro na gravação reportado pelo Arduino: {resposta} <<<", data);
                    _ = Task.Run(() => FalharGravacaoPendenteAsync(data));
                }
                else if (data.StartsWith("TIMEOUT:"))
                {
                    _logger.LogWarning(">>> Timeout na gravação reportado pelo Arduino: {resposta} <<<", data);
                    _ = Task.Run(() => FalharGravacaoPendenteAsync(data));
                }
                else if (data.StartsWith("UID:"))
                {
                    // UID físico do cartão
                    string uid = data.Replace("UID:", "");
                    _logger.LogInformation(">>> UID Físico lido: {uid} <<<", uid);
                    ProcessarLeituraCartao(novoUid: uid, novoGuid: null);
                }
                else if (data.StartsWith("GUID:"))
                {
                    // GUID gravado na memória do cartão
                    string guid = data.Replace("GUID:", "");
                    _logger.LogInformation(">>> GUID gravado lido: {guid} <<<", guid);
                    ProcessarLeituraCartao(novoUid: null, novoGuid: guid);
                }
                else
                {
                    // Dados diversos do Arduino (mensagens de status, etc.)
                    _logger.LogInformation(">>> Arduino: {data} <<<", data);
                }
            }
        }
        catch (IOException ioEx) when (ioEx.Message.Contains("aborted") || ioEx.Message.Contains("thread exit"))
        {
            // Ignora erros de I/O quando a aplicação está sendo finalizada
            if (!_isDisposing)
            {
                _logger.LogWarning("Operação de leitura da porta serial foi interrompida: {message}", ioEx.Message);
            }
        }
        catch (InvalidOperationException invEx) when (invEx.Message.Contains("port is closed"))
        {
            // Porta foi fechada durante a operação
            if (!_isDisposing)
            {
                _logger.LogWarning("Tentativa de leitura em porta serial fechada: {message}", invEx.Message);
            }
        }
        catch (TimeoutException)
        {
            // Timeout na leitura - não é necessário logar como erro
            _logger.LogDebug("Timeout na leitura da porta serial.");
        }
        catch (Exception ex)
        {
            if (!_isDisposing)
            {
                _logger.LogError(ex, "Erro inesperado ao ler dados da porta serial.");
            }
        }
    }

    private async Task ConfirmarGravacaoCartao(string? solicitacaoId, string? credencial)
    {
        if (string.IsNullOrWhiteSpace(solicitacaoId) || string.IsNullOrWhiteSpace(credencial))
        {
            _logger.LogError("Confirmação de gravação sem solicitação ou credencial válidas.");
            return;
        }

        var response = await _apiClient.ConfirmRecordingAsync(
            new ConfirmarGravacaoCartaoDto
            {
                SolicitacaoId = solicitacaoId,
                CredencialGravada = credencial
            });

        if (response == null)
        {
            _logger.LogError(
                "Não foi possível confirmar a solicitação {SolicitacaoId}; a solicitação permanece recuperável na API.",
                solicitacaoId);
            return;
        }

        using (response)
        {
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "Gravação confirmada pela API para solicitação {SolicitacaoId}.",
                    solicitacaoId);
            }
            else
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogError(
                    "A API recusou a confirmação da solicitação {SolicitacaoId}. Status: {Status}; Resposta: {Response}",
                    solicitacaoId, response.StatusCode, responseContent);
            }
        }
    }

    private async Task FalharGravacaoPendenteAsync(string motivo)
    {
        var solicitacaoId = _solicitacaoSendoGravada;
        _solicitacaoSendoGravada = null;
        _credencialSendoGravada = null;
        if (!string.IsNullOrWhiteSpace(solicitacaoId))
        {
            await ReportarFalhaGravacaoAsync(solicitacaoId, motivo);
        }
    }

    private async Task ReportarFalhaGravacaoAsync(string solicitacaoId, string motivo)
    {
        var response = await _apiClient.ReportRecordingFailureAsync(
            new FalharGravacaoCartaoDto
            {
                SolicitacaoId = solicitacaoId,
                Motivo = motivo.Length > 200 ? motivo[..200] : motivo
            });

        if (response == null)
        {
            _logger.LogError(
                "Não foi possível reportar falha da solicitação {SolicitacaoId}; ela permanece recuperável na API.",
                solicitacaoId);
            return;
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "A API recusou a falha da solicitação {SolicitacaoId}. Status: {Status}",
                    solicitacaoId, response.StatusCode);
            }
        }
    }

    private void ProcessarLeituraCartao(string? novoUid, string? novoGuid)
    {
        string? uidParaVerificar = null;
        string? guidParaVerificar = null;
        (DateTime Utc, long Ticks)? inicio = null;

        lock (_leituraLock)
        {
            MarcarInicioLeituraInterno();
            if (novoUid != null) _ultimoUidLido = novoUid;
            if (novoGuid != null) _ultimoGuidLido = novoGuid;

            if (!string.IsNullOrEmpty(_ultimoUidLido) && !string.IsNullOrEmpty(_ultimoGuidLido))
            {
                uidParaVerificar = _ultimoUidLido;
                guidParaVerificar = _ultimoGuidLido;
                inicio = ConsumirInicioLeituraInterno();
                LimparVariaveisTemporariasInterno();
            }
        }

        if (uidParaVerificar != null && guidParaVerificar != null)
        {
            _ = Task.Run(async () => 
            {
                // Se uma gravação recente foi realizada, aguardar a confirmação
                if (_gravacaoRecenteRealizada)
                {
                    _logger.LogInformation(">>> Aguardando confirmação de gravação antes de verificar acesso <<<");
                    while (_gravacaoRecenteRealizada)
                    {
                        await Task.Delay(500);
                    }
                }
                await VerificarAcesso(uidParaVerificar, guidParaVerificar, inicio);
            });
        }
    }

    private void MarcarInicioLeituraInterno()
    {
        if (_inicioLeituraUtc != null) return;
        _inicioLeituraUtc = DateTime.UtcNow;
        _inicioLeituraTicks = System.Diagnostics.Stopwatch.GetTimestamp();
        _logger.LogInformation(">>> [MEDICAO] Arduino enviou dados em {inicio:O} <<<", _inicioLeituraUtc);
    }

    private (DateTime Utc, long Ticks)? ConsumirInicioLeituraInterno()
    {
        if (_inicioLeituraUtc == null) return null;
        var inicio = (_inicioLeituraUtc.Value, _inicioLeituraTicks);
        _inicioLeituraUtc = null;
        _inicioLeituraTicks = 0;
        return inicio;
    }

    private void LimparVariaveisTemporariasInterno()
    {
        _ultimoUidLido = null;
        _ultimoGuidLido = null;
    }

    private async Task VerificarAcesso(string uid, string guid, (DateTime Utc, long Ticks)? inicioMedicao = null)
    {
        var inicio = inicioMedicao ?? default;
        var inicioApi = System.Diagnostics.Stopwatch.GetTimestamp();
        int? status = null;
        var autorizado = false;
        try
        {
            // Validar se os parâmetros não são nulos ou vazios
            if (string.IsNullOrEmpty(uid) || string.IsNullOrEmpty(guid))
            {
                _logger.LogWarning(">>> Tentativa de verificação de acesso com parâmetros inválidos - UID: {uid}, GUID: {guid} <<<", uid ?? "(null)", guid ?? "(null)");
                EnviarResultadoAcesso(AccessFeedbackSerialProtocol.AccessDenied);
                return;
            }

            _logger.LogInformation(">>> Verificando acesso para UID: {uid}, GUID: {guid} <<<", uid, guid);
            
            // Envia o UID físico para log/diagnóstico e o GUID gravado para validação da credencial
            using var response = await _apiClient.VerifyAccessAsync(
                uid: uid,
                guid: guid,
                local: _localAcesso);
            var responseContent = await response.Content.ReadAsStringAsync();
            status = (int)response.StatusCode;
            autorizado = response.IsSuccessStatusCode;
            RegistrarMedicao(inicio, inicioApi, status, autorizado);
            EnviarResultadoAcesso(AccessFeedbackSerialProtocol.ForResponse(response));
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(">>> ACESSO AUTORIZADO para GUID: {guid} (UID físico: {uid}) - Resposta: {response} <<<", guid, uid, responseContent);
            }
            else
            {
                _logger.LogWarning(">>> ACESSO NEGADO para GUID: {guid} (UID físico: {uid}) - Resposta: {response} <<<", guid, uid, responseContent);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao verificar acesso para UID: {uid}, GUID: {guid}", uid, guid);
            EnviarResultadoAcesso(AccessFeedbackSerialProtocol.AccessDenied);
        }
    }

    private void EnviarResultadoAcesso(string comando)
    {
        lock (_serialWriteLock)
        {
            var serialPort = _serialPort;
            if (serialPort?.IsOpen != true)
            {
                _logger.LogError(
                    "Não foi possível enviar o resultado {Comando} ao Arduino: porta serial indisponível.",
                    comando);
                return;
            }

            try
            {
                serialPort.WriteLine(comando);
                _logger.LogInformation("Resultado de acesso enviado ao Arduino pela serial: {Comando}", comando);
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "Falha de I/O ao enviar resultado {Comando} ao Arduino.", comando);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Porta serial fechada ao enviar resultado {Comando} ao Arduino.", comando);
            }
            catch (TimeoutException ex)
            {
                _logger.LogError(ex, "Timeout ao enviar resultado {Comando} ao Arduino.", comando);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Acesso negado ao enviar resultado {Comando} ao Arduino.", comando);
            }
        }
    }

    private void RegistrarMedicao((DateTime Utc, long Ticks) inicio, long inicioApiTicks, int? status, bool autorizado)
    {
        try
        {
            var fimTicks = System.Diagnostics.Stopwatch.GetTimestamp();
            var fimUtc = DateTime.UtcNow;
            var apiMs = System.Diagnostics.Stopwatch.GetElapsedTime(inicioApiTicks, fimTicks).TotalMilliseconds;
            if (inicio.Ticks == 0)
            {
                _logger.LogWarning(">>> [MEDICAO] Início da leitura não encontrado; medição descartada <<<");
                return;
            }

            var totalMs = System.Diagnostics.Stopwatch.GetElapsedTime(inicio.Ticks, fimTicks).TotalMilliseconds;
            _medicao.Registrar(new MedicaoRegistro(inicio.Utc, fimUtc, Math.Round(totalMs, 1), Math.Round(apiMs, 1), status, autorizado));
            _logger.LogInformation(">>> [MEDICAO] Resposta da API em {fim:O} - total {total:F0} ms (API {api:F0} ms) <<<", fimUtc, totalMs, apiMs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao registrar medição de tempo de resposta.");
        }
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker service parando...");
        _isDisposing = true;

        try
        {
            // Fechar conexão SignalR
            if (_hubConnection != null)
            {
                await _hubConnection.DisposeAsync();
                _logger.LogInformation("Conexão SignalR fechada com sucesso.");
            }

            if (_serialPort?.IsOpen == true)
            {
                _serialPort.DataReceived -= SerialPort_DataReceived;
                _serialPort.Close();
                _logger.LogInformation("Porta serial fechada com sucesso.");
            }
            _serialPort?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao fechar recursos durante o shutdown.");
        }

        await base.StopAsync(stoppingToken);
    }

    
}