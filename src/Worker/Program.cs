using ControleDeAcesso.Worker;

if (args.Length >= 1 && args[0] == "--resumo-medicoes")
{
    var caminho = args.Length > 1
        ? args[1]
        : Path.Combine(Directory.GetCurrentDirectory(), "src", "Worker", MedicaoTempoResposta.ArquivoPadrao);
    if (args.Length <= 1 && !File.Exists(caminho))
        caminho = Path.Combine(Directory.GetCurrentDirectory(), MedicaoTempoResposta.ArquivoPadrao);
    Console.WriteLine(MedicaoTempoResposta.Resumir(caminho));
    return;
}

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSingleton<MedicaoTempoResposta>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();