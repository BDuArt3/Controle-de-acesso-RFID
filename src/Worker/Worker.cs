namespace ControleDeAcesso.Worker;

using System.IO.Ports;
using Microsoft.AspNetCore.SignalR.Client;
public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _apiBaseUrl;
    private readonly string _comPortName;
    private SerialPort? _serialPort;
    private HubConnection? _hubConnection;
    private bool _isDisposing = false;
    
    // Variáveis para controle de leitura de acesso
    private string? _ultimoUidLido = null;
    private string? _ultimoGuidLido = null;

    public Worker(
        ILogger<Worker> logger,
        IConfiguration configuration
        )
    {
        _logger = logger;
        _httpClient = new HttpClient();

        _apiBaseUrl = configuration["ApiSettings:BaseUrl"] ?? "BaseUrl da API não informada";
        _comPortName = configuration["SerialPortSettings:ComPortName"] ?? "COM4";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker service iniciando...");

        // Configurar conexão SignalR primeiro
        await ConfigurarSignalRConnection();
        
        while (!stoppingToken.IsCancellationRequested)
        {
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
    private async Task ConfigurarSignalRConnection()
    {
        try
        {
            _hubConnection = new HubConnectionBuilder()
                .WithUrl($"{_apiBaseUrl}/gravacaoHub")
                .WithAutomaticReconnect()
                .Build();

            // Handler para receber comandos de gravação
            _hubConnection.On<string>("ReceberComandoGravacao", async (guid) =>
            {
                _logger.LogInformation(">>> Comando de gravação recebido para GUID: {guid} <<<", guid);
                await ProcessarComandoGravacao(guid);
            });

            await _hubConnection.StartAsync();
            _logger.LogInformation("Conexão SignalR estabelecida com sucesso em: {url}", $"{_apiBaseUrl}/gravacaoHub");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao estabelecer conexão SignalR com o Hub.");
        }
    }
    private async Task ProcessarComandoGravacao(string guid)
    {
        try
        {
            if (_serialPort?.IsOpen == true)
            {
                // Enviar apenas os primeiros 16 caracteres do GUID para o Arduino
                string guidReduzido = guid.Replace("-", "").Substring(0, 16);
                string comandoGravacao = $"WRITE:{guidReduzido}";
                _serialPort.WriteLine(comandoGravacao);
                
                _logger.LogInformation("Comando enviado para Arduino: {comando} (GUID original: {guidOriginal})", comandoGravacao, guid);
                
                // Aguardar processamento do Arduino (15s timeout + margem)
                await Task.Delay(17000);
            }
            else
            {
                _logger.LogWarning("Porta serial não está disponível para gravação.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar comando de gravação para GUID: {guid}", guid);
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
                }
                else if (data.StartsWith("ERROR:"))
                {
                    _logger.LogError(">>> Erro na gravação reportado pelo Arduino: {resposta} <<<", data);
                }
                else if (data.StartsWith("TIMEOUT:"))
                {
                    _logger.LogWarning(">>> Timeout na gravação reportado pelo Arduino: {resposta} <<<", data);
                }
                else if (data.StartsWith("UID:"))
                {
                    // UID físico do cartão
                    string uid = data.Replace("UID:", "");
                    _logger.LogInformation(">>> UID Físico lido: {uid} <<<", uid);
                    _ultimoUidLido = uid;
                    
                    // Se já temos tanto UID quanto GUID, verificar acesso
                    if (!string.IsNullOrEmpty(_ultimoGuidLido))
                    {
                        _ = Task.Run(() => VerificarAcesso(_ultimoUidLido, _ultimoGuidLido));
                        LimparVariaveisTemporarias();
                    }
                }
                else if (data.StartsWith("GUID:"))
                {
                    // GUID gravado na memória do cartão
                    string guid = data.Replace("GUID:", "");
                    _logger.LogInformation(">>> GUID gravado lido: {guid} <<<", guid);
                    _ultimoGuidLido = guid;
                    
                    // Se já temos tanto UID quanto GUID, verificar acesso
                    if (!string.IsNullOrEmpty(_ultimoUidLido))
                    {
                        _ = Task.Run(() => VerificarAcesso(_ultimoUidLido, _ultimoGuidLido));
                        LimparVariaveisTemporarias();
                    }
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

    private void LimparVariaveisTemporarias()
    {
        _ultimoUidLido = null;
        _ultimoGuidLido = null;
    }

    private async Task VerificarAcesso(string uid, string guid)
    {
        try
        {
            _logger.LogInformation(">>> Verificando acesso para UID: {uid}, GUID: {guid} <<<", uid, guid);
            
            // Fazer chamada para API de verificação de acesso
            var requestBody = new
            {
                uid = uid,
                guid = guid
            };
            
            var json = System.Text.Json.JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"{_apiBaseUrl}/api/acesso/verificar", content);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(">>> ACESSO AUTORIZADO para UID: {uid} - Resposta: {response} <<<", uid, responseContent);
            }
            else
            {
                _logger.LogWarning(">>> ACESSO NEGADO para UID: {uid} - Resposta: {response} <<<", uid, responseContent);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao verificar acesso para UID: {uid}, GUID: {guid}", uid, guid);
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

        _httpClient?.Dispose();

        await base.StopAsync(stoppingToken);
    }

    
}