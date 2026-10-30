using System.Net;
using System.Text.Json;
using ControleDeAcesso.Api.Security;
using ControleDeAcesso.Application.DTOs;
using ControleDeAcesso.Worker;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace ControleDeAcesso.Tests;

public sealed class WorkerApiClientTests
{
    private const string ApiKey = "worker-api-key-0123456789-32-bytes";

    [Fact]
    public async Task WorkerClientUsesProtectedRoutesAndSendsMachineKey()
    {
        var requests = new List<(string Path, string? ApiKey, string Body)>();
        using var client = new HttpClient(new RecordingHandler(async request =>
        {
            requests.Add((
                request.RequestUri!.PathAndQuery,
                request.Headers.TryGetValues(WorkerApiKeyAuthenticationHandler.HeaderName, out var values)
                    ? values.Single()
                    : null,
                await request.Content!.ReadAsStringAsync()));
            return new HttpResponseMessage(HttpStatusCode.OK);
        }));
        var api = new WorkerApiClient(client, "https://api.example.test/", ApiKey);

        using var confirmed = await api.ConfirmRecordingAsync(new ConfirmarGravacaoCartaoDto
        {
            SolicitacaoId = "request-1",
            CredencialGravada = "0123456789ABCDEF"
        });
        using var failed = await api.ReportRecordingFailureAsync(new FalharGravacaoCartaoDto
        {
            SolicitacaoId = "request-2",
            Motivo = "Hardware indisponível"
        });
        using var access = await api.VerifyAccessAsync(
            "0123456789ABCDEF",
            "credential-guid",
            "Portaria Bloco A");

        Assert.Equal(HttpStatusCode.OK, confirmed!.StatusCode);
        Assert.Equal(HttpStatusCode.OK, failed!.StatusCode);
        Assert.Equal(HttpStatusCode.OK, access.StatusCode);
        Assert.Collection(
            requests,
            request => AssertRequest(
                request,
                "/api/cartao/confirmar-gravacao",
                ApiKey,
                ("solicitacaoId", "request-1"),
                ("credencialGravada", "0123456789ABCDEF")),
            request => AssertRequest(
                request,
                "/api/cartao/falhar-gravacao",
                ApiKey,
                ("solicitacaoId", "request-2"),
                ("motivo", "Hardware indisponível")),
            request => AssertRequest(
                request,
                "/api/acesso/verificar",
                ApiKey,
                ("uid", "0123456789ABCDEF"),
                ("guid", "credential-guid"),
                ("local", "Portaria Bloco A")));
    }

    [Fact]
    public async Task ApiKeyHandlerAuthenticatesOnlyMatchingConfiguredWorkerKey()
    {
        var handler = CreateHandler(ApiKey);
        var context = new DefaultHttpContext();
        context.Request.Headers[WorkerApiKeyAuthenticationHandler.HeaderName] = ApiKey;
        await handler.InitializeAsync(
            new AuthenticationScheme(
                WorkerApiKeyAuthenticationHandler.SchemeName,
                WorkerApiKeyAuthenticationHandler.SchemeName,
                typeof(WorkerApiKeyAuthenticationHandler)),
            context);

        var result = await handler.AuthenticateAsync();

        Assert.True(result.Succeeded);
        Assert.Equal("rfid-worker", result.Principal!.Identity!.Name);
    }

    [Fact]
    public async Task ApiKeyHandlerRejectsWrongWorkerKey()
    {
        var handler = CreateHandler(ApiKey);
        var context = new DefaultHttpContext();
        context.Request.Headers[WorkerApiKeyAuthenticationHandler.HeaderName] = "wrong-worker-key";
        await handler.InitializeAsync(
            new AuthenticationScheme(
                WorkerApiKeyAuthenticationHandler.SchemeName,
                WorkerApiKeyAuthenticationHandler.SchemeName,
                typeof(WorkerApiKeyAuthenticationHandler)),
            context);

        var result = await handler.AuthenticateAsync();

        Assert.False(result.Succeeded);
        Assert.NotNull(result.Failure);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-url")]
    [InlineData("ftp://worker.example.test")]
    public void WorkerRejectsMissingOrInvalidApiBaseUrl(string apiBaseUrl)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ApiSettings:BaseUrl"] = apiBaseUrl,
                ["WorkerAuth:ApiKey"] = ApiKey
            })
            .Build();

        Assert.Throws<InvalidOperationException>(() =>
            new ControleDeAcesso.Worker.Worker(
                NullLogger<ControleDeAcesso.Worker.Worker>.Instance,
                configuration,
                new ControleDeAcesso.Worker.MedicaoTempoResposta(
                    configuration,
                    Mock.Of<IHostEnvironment>(e => e.ContentRootPath == AppContext.BaseDirectory))));
    }

    [Fact]
    public void InventoryScriptIsReadOnlyAndIncludesOrphanedLegacyLogClassification()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "PreMigrationInventory.sql");
        var sql = File.ReadAllText(path);
        var executableSql = System.Text.RegularExpressions.Regex.Replace(sql, @"/\*.*?\*/|--[^\r\n]*", string.Empty,
            System.Text.RegularExpressions.RegexOptions.Singleline);

        Assert.DoesNotMatch(
            @"(?i)\b(INSERT|UPDATE|DELETE|ALTER|DROP|CREATE|MERGE)\b",
            executableSql);
        Assert.Contains("LogsUidSemCartao", executableSql, StringComparison.Ordinal);
        Assert.Contains("LEFT JOIN dbo.Cartoes AS c ON c.Uid = l.CartaoUid", executableSql, StringComparison.Ordinal);
    }

    private static void AssertRequest(
        (string Path, string? ApiKey, string Body) request,
        string expectedPath,
        string expectedApiKey,
        params (string Name, string Value)[] expectedPayload)
    {
        Assert.Equal(expectedPath, request.Path);
        Assert.Equal(expectedApiKey, request.ApiKey);
        using var json = JsonDocument.Parse(request.Body);
        foreach (var (name, value) in expectedPayload)
        {
            Assert.Equal(value, json.RootElement.GetProperty(name).GetString());
        }
    }

    private static WorkerApiKeyAuthenticationHandler CreateHandler(string configuredKey)
    {
        var options = new Mock<IOptionsMonitor<AuthenticationSchemeOptions>>();
        options.Setup(monitor => monitor.Get(It.IsAny<string>()))
            .Returns(new AuthenticationSchemeOptions());
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["WorkerAuth:ApiKey"] = configuredKey
            })
            .Build();
        return new WorkerApiKeyAuthenticationHandler(
            options.Object,
            NullLoggerFactory.Instance,
            System.Text.Encodings.Web.UrlEncoder.Default,
            configuration);
    }

    private sealed class RecordingHandler(
        Func<HttpRequestMessage, Task<HttpResponseMessage>> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) => responder(request);
    }
}
