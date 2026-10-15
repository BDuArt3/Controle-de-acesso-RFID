using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace ControleDeAcesso.Api.Security;

public sealed class WorkerApiKeyAuthenticationHandler
    : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "RfidWorker";
    public const string HeaderName = "X-Worker-Key";

    private readonly IConfiguration _configuration;

    public WorkerApiKeyAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IConfiguration configuration)
        : base(options, logger, encoder)
    {
        _configuration = configuration;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var configuredKey = _configuration["WorkerAuth:ApiKey"];
        if (string.IsNullOrWhiteSpace(configuredKey) || configuredKey.Length < 32)
        {
            return Task.FromResult(AuthenticateResult.Fail(
                "Worker authentication is not configured with a key of at least 32 characters."));
        }

        if (!Request.Headers.TryGetValue(HeaderName, out var providedValues) ||
            providedValues.Count != 1 ||
            string.IsNullOrEmpty(providedValues[0]))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var configuredBytes = Encoding.UTF8.GetBytes(configuredKey);
        var providedBytes = Encoding.UTF8.GetBytes(providedValues[0]!);
        if (!CryptographicOperations.FixedTimeEquals(configuredBytes, providedBytes))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid worker credentials."));
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "rfid-worker"),
            new Claim(ClaimTypes.Name, "rfid-worker"),
            new Claim("machine_type", "rfid-worker")
        };
        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
