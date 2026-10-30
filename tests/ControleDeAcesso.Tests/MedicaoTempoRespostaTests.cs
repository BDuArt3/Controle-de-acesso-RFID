using ControleDeAcesso.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Moq;
using System.Text.Json;
using Xunit;

namespace ControleDeAcesso.Tests;

public sealed class MedicaoTempoRespostaTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly string _filePath;

    public MedicaoTempoRespostaTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "MedicaoTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
        _filePath = Path.Combine(_tempDirectory, "medicoes.json");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            try
            {
                Directory.Delete(_tempDirectory, recursive: true);
            }
            catch
            {
                // Ignora falhas de cleanup em testes
            }
        }
    }

    private MedicaoTempoResposta CreateSut(bool habilitada = true)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Medicao:Habilitada"] = habilitada.ToString(),
                ["Medicao:CaminhoArquivo"] = _filePath
            })
            .Build();

        var env = Mock.Of<IHostEnvironment>(e => e.ContentRootPath == _tempDirectory);
        return new MedicaoTempoResposta(config, env);
    }

    [Fact]
    public void Registrar_AppendsSingleLineInJsonLinesFormat()
    {
        var sut = CreateSut();
        var inicio = new DateTime(2026, 10, 29, 12, 0, 0, DateTimeKind.Utc);

        var reg1 = new MedicaoRegistro(inicio, inicio.AddMilliseconds(500), 500, 480, 200, true);
        var reg2 = new MedicaoRegistro(inicio.AddSeconds(1), inicio.AddSeconds(1).AddMilliseconds(600), 600, 580, 200, true);

        sut.Registrar(reg1);
        sut.Registrar(reg2);

        Assert.True(File.Exists(_filePath));
        var lines = File.ReadAllLines(_filePath);
        Assert.Equal(2, lines.Length);

        // Cada linha deve ser um JSON válido e autônomo
        var parsed1 = JsonSerializer.Deserialize<MedicaoRegistro>(lines[0]);
        var parsed2 = JsonSerializer.Deserialize<MedicaoRegistro>(lines[1]);
        Assert.NotNull(parsed1);
        Assert.NotNull(parsed2);
        Assert.Equal(500, parsed1.TempoTotalMs);
        Assert.Equal(600, parsed2.TempoTotalMs);

        var carregados = MedicaoTempoResposta.CarregarRegistros(_filePath);
        Assert.Equal(2, carregados.Count);
    }

    [Fact]
    public void Registrar_MigratesLegacyJsonArrayToJsonLines()
    {
        var inicio = new DateTime(2026, 10, 29, 10, 0, 0, DateTimeKind.Utc);
        var legados = new List<MedicaoRegistro>
        {
            new(inicio, inicio.AddMilliseconds(400), 400, 390, 200, true),
            new(inicio.AddSeconds(2), inicio.AddSeconds(2).AddMilliseconds(700), 700, 690, 403, false)
        };

        // Salva formato legado (array formatado com [])
        File.WriteAllText(_filePath, JsonSerializer.Serialize(legados, new JsonSerializerOptions { WriteIndented = true }));

        var sut = CreateSut();
        var novoReg = new MedicaoRegistro(inicio.AddSeconds(4), inicio.AddSeconds(4).AddMilliseconds(550), 550, 540, 200, true);
        sut.Registrar(novoReg);

        var lines = File.ReadAllLines(_filePath);
        Assert.Equal(3, lines.Length);
        Assert.DoesNotContain("[", lines[0]);

        var registros = MedicaoTempoResposta.CarregarRegistros(_filePath);
        Assert.Equal(3, registros.Count);
        Assert.Equal(400, registros[0].TempoTotalMs);
        Assert.Equal(700, registros[1].TempoTotalMs);
        Assert.Equal(550, registros[2].TempoTotalMs);
    }

    [Fact]
    public void Resumir_CalculatesStatisticsCorrectly()
    {
        var sut = CreateSut();
        var inicio = DateTime.UtcNow;
        sut.Registrar(new MedicaoRegistro(inicio, inicio.AddMilliseconds(200), 200, 190, 200, true));
        sut.Registrar(new MedicaoRegistro(inicio, inicio.AddMilliseconds(400), 400, 390, 200, true));
        sut.Registrar(new MedicaoRegistro(inicio, inicio.AddMilliseconds(600), 600, 590, 200, true));

        var resumo = MedicaoTempoResposta.Resumir(_filePath);

        Assert.Contains("Leituras: 3", resumo);
        Assert.Contains("Média: 400 ms", resumo);
        Assert.Contains("Mínimo: 200 ms", resumo);
        Assert.Contains("Máximo: 600 ms", resumo);
    }

    [Fact]
    public void Registrar_DoesNothing_WhenDisabled()
    {
        var sut = CreateSut(habilitada: false);
        var inicio = DateTime.UtcNow;

        sut.Registrar(new MedicaoRegistro(inicio, inicio.AddMilliseconds(500), 500, 490, 200, true));

        Assert.False(File.Exists(_filePath));
    }

    [Fact]
    public void Resumir_ReturnsInformativeError_WhenLegacyJsonArrayIsCorrupted()
    {
        // JSON array truncado ou com sintaxe quebrada
        File.WriteAllText(_filePath, "[ { \"InicioUtc\": \"2026-10-29\", \"TempoTotalMs\": 500, ");

        var resumo = MedicaoTempoResposta.Resumir(_filePath);

        Assert.NotNull(resumo);
        Assert.Contains("JSON inválido ou corrompido", resumo);
    }

    [Fact]
    public void Resumir_RecoversValidLinesAndReportsDamagedRecords_WhenJsonLinesHasCorruptedLines()
    {
        var inicio = DateTime.UtcNow;
        var reg1 = JsonSerializer.Serialize(new MedicaoRegistro(inicio, inicio.AddMilliseconds(300), 300, 290, 200, true));
        var reg2 = JsonSerializer.Serialize(new MedicaoRegistro(inicio, inicio.AddMilliseconds(500), 500, 490, 200, true));

        // Linha 1 válida, linha 2 corrompida, linha 3 válida
        File.WriteAllText(_filePath, reg1 + Environment.NewLine + "NOT_A_VALID_JSON_LINE" + Environment.NewLine + reg2 + Environment.NewLine);

        var resumo = MedicaoTempoResposta.Resumir(_filePath);

        Assert.Contains("Leituras: 2", resumo);
        Assert.Contains("Média: 400 ms", resumo);
        Assert.Contains("1 registro(s) inválido(s) desconsiderado(s)", resumo);
    }

    [Fact]
    public void Resumir_ReturnsInformativeError_WhenAllLinesAreCorrupted()
    {
        File.WriteAllText(_filePath, "corrupted_line_1\ncorrupted_line_2\n");

        var resumo = MedicaoTempoResposta.Resumir(_filePath);

        Assert.Contains("todos os registros continham JSON corrompido ou inválido", resumo);
    }
}
