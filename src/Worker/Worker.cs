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
    
    // Controle de gravações pendentes
    private readonly Dictionary<string, string> _gravacoesEntreAndamento = new(); // GUID -> UsuarioId
    private string? _guidSendoGravado = null;
    private bool _gravacaoRecenteRealizada = false;

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
                
                // Armazenar o GUID sendo gravado para controle
                _guidSendoGravado = guidReduzido;
                
                _serialPort.WriteLine(comandoGravacao);
                
                _logger.LogInformation("Comando enviado para Arduino: {comando} (GUID original: {guidOriginal})", comandoGravacao, guid);
                
                // Aguardar processamento do Arduino (15s timeout + margem)
                await Task.Delay(17000);
                
                // Limpar GUID sendo gravado se não houve resposta
                if (_guidSendoGravado == guidReduzido)
                {
                    _guidSendoGravado = null;
                    _logger.LogWarning("Timeout na gravação - limpando GUID sendo gravado: {guid}", guidReduzido);
                }
            }
            else
            {
                _logger.LogWarning("Porta serial não está disponível para gravação.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar comando de gravação para GUID: {guid}", guid);
            _guidSendoGravado = null;
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
                    if (!string.IsNullOrEmpty(_guidSendoGravado))
                    {
                        var guidParaConfirmar = _guidSendoGravado;
                        _guidSendoGravado = null;
                        _ = Task.Run(async () => 
                        {
                            await ConfirmarGravacaoCartao(guidParaConfirmar);
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
                        var uidParaVerificar = _ultimoUidLido;
                        var guidParaVerificar = _ultimoGuidLido;
                        LimparVariaveisTemporarias();
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
                            await VerificarAcesso(uidParaVerificar, guidParaVerificar);
                        });
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
                        var uidParaVerificar = _ultimoUidLido;
                        var guidParaVerificar = _ultimoGuidLido;
                        LimparVariaveisTemporarias();
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
                            await VerificarAcesso(uidParaVerificar, guidParaVerificar);
                        });
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

    private async Task ConfirmarGravacaoCartao(string guidGravado)
    {
        try
        {
            // Validar se o GUID não é nulo ou vazio
            if (string.IsNullOrEmpty(guidGravado))
            {
                _logger.LogWarning(">>> Tentativa de confirmação de gravação com GUID inválido: {guid} <<<", guidGravado ?? "(null)");
                return;
            }

            _logger.LogInformation(">>> Confirmando gravação de cartão para GUID: {guid} <<<", guidGravado);
            
            // Fazer chamada para API para confirmar a gravação
            var requestBody = new
            {
                guidGravado = guidGravado
            };
            
            var json = System.Text.Json.JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"{_apiBaseUrl}/api/cartao/confirmar-gravacao", content);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(">>> Gravação de cartão confirmada com sucesso - GUID: {guid} - Resposta: {response} <<<", guidGravado, responseContent);
            }
            else
            {
                _logger.LogError(">>> Erro ao confirmar gravação de cartão - GUID: {guid} - Status: {status} - Resposta: {response} <<<", 
                    guidGravado, response.StatusCode, responseContent);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ">>> Erro inesperado ao confirmar gravação de cartão para GUID: {guid} <<<", guidGravado);
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
            // Validar se os parâmetros não são nulos ou vazios
            if (string.IsNullOrEmpty(uid) || string.IsNullOrEmpty(guid))
            {
                _logger.LogWarning(">>> Tentativa de verificação de acesso com parâmetros inválidos - UID: {uid}, GUID: {guid} <<<", uid ?? "(null)", guid ?? "(null)");
                return;
            }

            _logger.LogInformation(">>> Verificando acesso para UID: {uid}, GUID: {guid} <<<", uid, guid);
            
            // Usar o GUID gravado no cartão para verificação de acesso, não o UID físico
            var requestBody = new
            {
                uid = guid, // Usar o GUID como identificador principal
                guid = guid
            };
            
            var json = System.Text.Json.JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            
            var response = await _httpClient.PostAsync($"{_apiBaseUrl}/api/acesso/verificar", content);
            var responseContent = await response.Content.ReadAsStringAsync();
            
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