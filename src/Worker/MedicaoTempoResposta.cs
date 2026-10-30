namespace ControleDeAcesso.Worker;

using System.Text.Json;

public sealed record MedicaoRegistro(
    DateTime InicioUtc,
    DateTime FimUtc,
    double TempoTotalMs,
    double TempoApiMs,
    int? StatusHttp,
    bool Autorizado);

// Registra medições de tempo de resposta em formato append-friendly (JSON Lines).
// Cada linha do arquivo corresponde a um registro JSON autônomo, evitando leitura e reescrita completa a cada acesso.
public sealed class MedicaoTempoResposta
{
    private static readonly JsonSerializerOptions JsonOptions = new();
    private readonly object _lock = new();
    private readonly string _caminho;
    private readonly bool _habilitada;

    public MedicaoTempoResposta(IConfiguration configuration, IHostEnvironment env)
    {
        _habilitada = configuration.GetValue("Medicao:Habilitada", true);
        _caminho = configuration["Medicao:CaminhoArquivo"]
            ?? Path.Combine(env.ContentRootPath, ArquivoPadrao);
    }

    public const string ArquivoPadrao = "medicoes-tempo-resposta.json";

    public void Registrar(MedicaoRegistro registro)
    {
        if (!_habilitada) return;

        lock (_lock)
        {
            GarantirFormatoJsonLines(_caminho);
            var linha = JsonSerializer.Serialize(registro, JsonOptions) + Environment.NewLine;
            File.AppendAllText(_caminho, linha);
        }
    }

    public static List<MedicaoRegistro> CarregarRegistros(string caminho)
    {
        try
        {
            return CarregarRegistrosComDiagnostico(caminho).Registros;
        }
        catch (JsonException)
        {
            return new();
        }
    }

    public static (List<MedicaoRegistro> Registros, int LinhasCorrompidas) CarregarRegistrosComDiagnostico(string caminho)
    {
        if (!File.Exists(caminho)) return (new(), 0);

        var primeiroChar = ObterPrimeiroCaractereNaoVazio(caminho);
        if (primeiroChar == '[')
        {
            var texto = File.ReadAllText(caminho);
            var registros = JsonSerializer.Deserialize<List<MedicaoRegistro>>(texto);
            return (registros ?? new(), 0);
        }

        var lista = new List<MedicaoRegistro>();
        var corrompidas = 0;
        foreach (var linha in File.ReadLines(caminho))
        {
            if (string.IsNullOrWhiteSpace(linha)) continue;
            try
            {
                var item = JsonSerializer.Deserialize<MedicaoRegistro>(linha, JsonOptions);
                if (item != null) lista.Add(item);
                else corrompidas++;
            }
            catch (JsonException)
            {
                corrompidas++;
            }
        }
        return (lista, corrompidas);
    }

    public static string Resumir(string caminho)
    {
        try
        {
            if (!File.Exists(caminho))
                return $"Arquivo não encontrado: {Path.GetFullPath(caminho)}. Ele é criado após as primeiras leituras.";

            var fileInfo = new FileInfo(caminho);
            if (fileInfo.Length == 0)
                return "Sem medições (arquivo vazio).";

            var (lista, linhasCorrompidas) = CarregarRegistrosComDiagnostico(caminho);

            if (lista.Count == 0)
            {
                if (linhasCorrompidas > 0)
                    return $"Erro ao processar medições em '{Path.GetFullPath(caminho)}': todos os registros continham JSON corrompido ou inválido.";

                return "Sem medições.";
            }

            var t = lista.Select(m => m.TempoTotalMs).ToList();
            var resumo = $"Leituras: {t.Count} | Média: {t.Average():F0} ms | Mínimo: {t.Min():F0} ms | Máximo: {t.Max():F0} ms";
            if (linhasCorrompidas > 0)
            {
                resumo += $" ({linhasCorrompidas} registro(s) inválido(s) desconsiderado(s))";
            }
            return resumo;
        }
        catch (JsonException ex)
        {
            return $"Erro ao ler arquivo de medições em '{Path.GetFullPath(caminho)}': JSON inválido ou corrompido ({ex.Message}).";
        }
        catch (Exception ex)
        {
            return $"Erro ao ler arquivo de medições em '{Path.GetFullPath(caminho)}': {ex.Message}";
        }
    }

    private static void GarantirFormatoJsonLines(string caminho)
    {
        if (!File.Exists(caminho)) return;

        var primeiroChar = ObterPrimeiroCaractereNaoVazio(caminho);
        if (primeiroChar != '[') return;

        try
        {
            var registros = JsonSerializer.Deserialize<List<MedicaoRegistro>>(File.ReadAllText(caminho)) ?? new();
            var temp = caminho + ".tmp";
            using (var writer = new StreamWriter(temp, append: false))
            {
                foreach (var reg in registros)
                {
                    writer.WriteLine(JsonSerializer.Serialize(reg, JsonOptions));
                }
            }
            File.Move(temp, caminho, overwrite: true);
        }
        catch (JsonException)
        {
            File.Move(caminho, caminho + $".corrompido-{DateTime.UtcNow:yyyyMMddHHmmss}");
        }
    }

    private static char? ObterPrimeiroCaractereNaoVazio(string caminho)
    {
        using var stream = new FileStream(caminho, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(stream);
        while (!reader.EndOfStream)
        {
            var ch = (char)reader.Read();
            if (!char.IsWhiteSpace(ch)) return ch;
        }
        return null;
    }
}
