using ControleDeAcesso.Application.DTOs;
using ControleDeAcesso.Application.Repositories.Interfaces;
using ControleDeAcesso.Application.Services;
using ControleDeAcesso.Application.Services.Interfaces;
using ControleDeAcesso.Core.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace ControleDeAcesso.Tests;

public sealed class AccessAttemptTests
{
    private const string Credential = "0123456789ABCDEF";
    private const string CardId = "card-id";
    private const string UserId = "user-id";

    [Fact]
    public async Task AllowedAttemptLogsCardAndUserSnapshotBeforeAllowing()
    {
        var fixture = new AccessFixture();
        fixture.CardRepository
            .Setup(repository => repository.GetByUidAsync(Credential))
            .ReturnsAsync(CreateCard(StatusCartao.Ativo, userActive: true));

        var response = await fixture.Service.CheckAccessAsync(Credential);

        Assert.True(response.Allowed);
        var logged = Assert.Single(fixture.Logs);
        Assert.Equal(CardId, logged.CartaoId);
        Assert.Equal(UserId, logged.UsuarioId);
        Assert.Null(logged.UidInformado);
        Assert.True(logged.AcessoPermitido);
    }

    [Theory]
    [InlineData(StatusCartao.Bloqueado, true, TipoMotivoAcesso.CartaoDesativado)]
    [InlineData(StatusCartao.Ativo, false, TipoMotivoAcesso.UsuarioDesativado)]
    public async Task DeniedKnownAttemptLogsCardAndUserSnapshot(
        StatusCartao status,
        bool userActive,
        TipoMotivoAcesso expectedReason)
    {
        var fixture = new AccessFixture();
        fixture.CardRepository
            .Setup(repository => repository.GetByUidAsync(Credential))
            .ReturnsAsync(CreateCard(status, userActive));

        var response = await fixture.Service.CheckAccessAsync(Credential);

        Assert.False(response.Allowed);
        var logged = Assert.Single(fixture.Logs);
        Assert.Equal(CardId, logged.CartaoId);
        Assert.Equal(UserId, logged.UsuarioId);
        Assert.Equal(expectedReason, logged.MotivoTipo);
        Assert.False(logged.AcessoPermitido);
    }

    [Fact]
    public async Task UnknownCredentialIsRecordedWithoutForeignKeys()
    {
        var fixture = new AccessFixture();

        var response = await fixture.Service.CheckAccessAsync(Credential);

        Assert.False(response.Allowed);
        var logged = Assert.Single(fixture.Logs);
        Assert.Null(logged.CartaoId);
        Assert.Null(logged.UsuarioId);
        Assert.Equal(Credential, logged.UidInformado);
        Assert.Equal(TipoMotivoAcesso.CartaoNaoEncontrado, logged.MotivoTipo);
    }

    [Fact]
    public async Task EmptyCredentialIsLoggedWithoutIdentifiers()
    {
        var fixture = new AccessFixture();

        var response = await fixture.Service.CheckAccessAsync("  ");

        Assert.False(response.Allowed);
        var logged = Assert.Single(fixture.Logs);
        Assert.Null(logged.CartaoId);
        Assert.Null(logged.UsuarioId);
        Assert.Null(logged.UidInformado);
        Assert.Equal(TipoMotivoAcesso.UidObrigatorio, logged.MotivoTipo);
    }

    [Fact]
    public async Task AuditFailureNeverReturnsAllowed()
    {
        var fixture = new AccessFixture();
        fixture.CardRepository
            .Setup(repository => repository.GetByUidAsync(Credential))
            .ReturnsAsync(CreateCard(StatusCartao.Ativo, userActive: true));
        fixture.LogService
            .Setup(service => service.RegistrarTentativaAsync(It.IsAny<CreateLogAcessoDto>()))
            .ThrowsAsync(new InvalidOperationException("DB unavailable"));

        var response = await fixture.Service.CheckAccessAsync(Credential);

        Assert.False(response.Allowed);
        Assert.Contains("auditoria", response.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static Cartao CreateCard(StatusCartao status, bool userActive)
    {
        var user = new Usuario
        {
            Id = UserId,
            Nome = "Pessoa",
            Documento = "document",
            IsActive = userActive
        };
        var card = new Cartao { Id = CardId, Uid = Credential, UsuarioId = UserId };
        if (status != StatusCartao.Ativo)
        {
            card.AlterarStatus(status);
        }

        typeof(Cartao).GetProperty(nameof(Cartao.Usuario))!.SetValue(card, user);
        return card;
    }

    private sealed class AccessFixture
    {
        public Mock<ICartaoRepository> CardRepository { get; } = new();
        public Mock<ILogAcessoService> LogService { get; } = new();
        public List<CreateLogAcessoDto> Logs { get; } = [];
        public AcessoService Service { get; }

        public AccessFixture()
        {
            CardRepository.Setup(repository => repository.GetByUidAsync(It.IsAny<string>()))
                .ReturnsAsync((Cartao?)null);
            LogService
                .Setup(service => service.RegistrarTentativaAsync(It.IsAny<CreateLogAcessoDto>()))
                .Callback<CreateLogAcessoDto>(Logs.Add)
                .ReturnsAsync((LogAcessoDto)null!);
            Service = new AcessoService(
                CardRepository.Object,
                LogService.Object,
                NullLogger<AcessoService>.Instance);
        }
    }
}
