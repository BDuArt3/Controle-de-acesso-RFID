namespace ControleDeAcesso.Worker;

using System.IO.Ports;
public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _apiBaseUrl;
    private readonly string _comPortName;
    private SerialPort? _serialPort;
    private bool _isDisposing = false;

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
        catch (Exception ex)
        {
            _logger.LogError(ex, "ERRO: Nao foi possivel abrir a porta serial '{portName}'. Verifique se a porta esta correta, disponivel e se o Arduino esta conectado.", _comPortName);
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }

    private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        if (_serialPort == null || !_serialPort.IsOpen || _isDisposing) return;

        try
        {
            string uid = _serialPort.ReadLine().Trim();

            if (!string.IsNullOrEmpty(uid))
            {
                _logger.LogInformation(">>> UID Recebido do Leitor: {uid} <<<", uid);
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
    
    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker service parando...");
        _isDisposing = true;
        
        try
        {
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
            _logger.LogWarning(ex, "Erro ao fechar a porta serial durante o shutdown.");
        }
        
        _httpClient?.Dispose();
        
        await base.StopAsync(stoppingToken);
    }
}