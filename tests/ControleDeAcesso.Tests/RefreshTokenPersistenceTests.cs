using System.Security.Cryptography;
using System.Text;
using ControleDeAcesso.Application.DTOs;
using ControleDeAcesso.Application.Services;
using ControleDeAcesso.Core.Entities;
using ControleDeAcesso.Infrastructure.Data;
using ControleDeAcesso.Infrastructure.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace ControleDeAcesso.Tests;

public sealed class RefreshTokenPersistenceTests : IAsyncLifetime
{
    private readonly string _databasePath = Path.Combine(
        Path.GetTempPath(),
        $"refresh-token-tests-{Guid.NewGuid():N}.db");
    private DbContextOptions<AppDbContext> _options = null!;
    private readonly IConfiguration _configuration = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:SecretKey"] = "test-secret-key-for-refresh-token-tests-at-least-32-bytes",
            ["Jwt:Issuer"] = "tests",
            ["Jwt:Audience"] = "tests",
            ["Jwt:ExpirationInMinutes"] = "15"
        })
        .Build();

    public async Task InitializeAsync()
    {
        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={_databasePath};Pooling=False")
            .Options;

        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync();
        var usuario = new Usuario
        {
            Id = "usuario-auth",
            Nome = "Operador de teste",
            Email = "operador@example.test",
            Tipo = TipoUsuario.Colaborador,
            IsActive = true
        };
        context.Usuarios.Add(usuario);
        context.Funcionarios.Add(new Funcionario
        {
            Id = "funcionario-auth",
            UsuarioId = usuario.Id,
            Usuario = usuario,
            SenhaHash = BCrypt.Net.BCrypt.HashPassword("senha-segura"),
            Departamento = "Teste",
            Cargo = "Operador",
            Tipo = TipoFuncionario.Operador,
            IsActive = true
        });
        await context.SaveChangesAsync();
    }

    public Task DisposeAsync()
    {
        if (File.Exists(_databasePath))
        {
            File.Delete(_databasePath);
        }

        return Task.CompletedTask;
    }

    [Fact]
    public async Task LoginRefreshAndRevokePersistAcrossAuthServiceInstances()
    {
        LoginResponseDto login;
        await using (var context = CreateContext())
        {
            login = await CreateAuthService(context).LoginAsync(new LoginDto
            {
                Email = "operador@example.test",
                Senha = "senha-segura"
            });
        }

        Assert.True(login.Success);
        var rawRefreshToken = Assert.IsType<TokenDto>(login.Token).RefreshToken;
        var originalHash = Hash(rawRefreshToken);
        await using (var context = CreateContext())
        {
            var stored = await context.RefreshTokens.SingleAsync();
            Assert.Equal(originalHash, stored.TokenHash);
            Assert.NotEqual(rawRefreshToken, stored.TokenHash);
            Assert.Null(stored.RevokedAtUtc);
        }

        TokenDto rotated;
        await using (var context = CreateContext())
        {
            rotated = await CreateAuthService(context).RefreshTokenAsync(rawRefreshToken);
        }

        Assert.NotEqual(rawRefreshToken, rotated.RefreshToken);
        var newHash = Hash(rotated.RefreshToken);
        await using (var context = CreateContext())
        {
            var previous = await context.RefreshTokens.SingleAsync(token => token.TokenHash == originalHash);
            var replacement = await context.RefreshTokens.SingleAsync(token => token.TokenHash == newHash);
            Assert.NotNull(previous.RevokedAtUtc);
            Assert.Equal(newHash, previous.ReplacedByTokenHash);
            Assert.Null(replacement.RevokedAtUtc);
        }

        await using (var context = CreateContext())
        {
            var service = CreateAuthService(context);
            await Assert.ThrowsAsync<SecurityTokenException>(
                () => service.RefreshTokenAsync(rawRefreshToken));
            Assert.True(await service.RevokeTokenAsync(rotated.RefreshToken));
        }

        await using (var context = CreateContext())
        {
            var revoked = await context.RefreshTokens.SingleAsync(token => token.TokenHash == newHash);
            Assert.NotNull(revoked.RevokedAtUtc);
            await Assert.ThrowsAsync<SecurityTokenException>(
                () => CreateAuthService(context).RefreshTokenAsync(rotated.RefreshToken));
        }
    }

    [Fact]
    public async Task RefreshTokenAsyncRejectsAndRevokesExpiredPersistedToken()
    {
        const string rawToken = "expired-test-refresh-token";
        var now = DateTime.UtcNow;
        var hash = Hash(rawToken);
        await using (var context = CreateContext())
        {
            context.RefreshTokens.Add(new RefreshToken
            {
                TokenHash = hash,
                FuncionarioId = "funcionario-auth",
                CreatedAtUtc = now.AddDays(-8),
                ExpiresAtUtc = now.AddSeconds(-1)
            });
            await context.SaveChangesAsync();
        }

        await using (var context = CreateContext())
        {
            await Assert.ThrowsAsync<SecurityTokenException>(
                () => CreateAuthService(context).RefreshTokenAsync(rawToken));
        }

        await using (var context = CreateContext())
        {
            Assert.NotNull((await context.RefreshTokens.SingleAsync()).RevokedAtUtc);
        }
    }

    private AppDbContext CreateContext() => new(_options);

    private AuthService CreateAuthService(AppDbContext context) =>
        new(
            new FuncionarioRepository(context, NullLogger<FuncionarioRepository>.Instance),
            new RefreshTokenRepository(context),
            _configuration,
            NullLogger<AuthService>.Instance);

    private static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
