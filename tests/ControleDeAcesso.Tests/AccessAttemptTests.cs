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
    public async Task AllowedAttemptLogsCardUserAndLocationBeforeAllowing()
    {
        var fixture = new AccessFixture();
        fixture.CardRepository
            .Setup(repository => repository.GetByUidAsync(Credential))
            .ReturnsAsync(CreateCard(StatusCartao.Ativo, userActive: true));

        var response = await fixture.Service.CheckAccessAsync(Credential, "Bloco A");

        Assert.True(response.Allowed);
        var logged = Assert.Single(fixture.Logs);
        Assert.Equal(CardId, logged.CartaoId);
        Assert.Equal(UserId, logged.UsuarioId);
        Assert.Equal("Bloco A", logged.Local);
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
        Assert.Equal(LogAcesso.LocalPadrao, logged.Local);
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

    [Fact]
    public async Task ExpiredVisitorCardIsBlockedAndAccessIsDenied()
    {
        var fixture = new AccessFixture();
        var card = CreateCard(
            StatusCartao.Ativo,
            userActive: true,
            tipo: TipoUsuario.Visitante,
            expiration: DateTime.UtcNow.AddSeconds(-1));
        fixture.CardRepository
            .Setup(repository => repository.GetByUidAsync(Credential))
            .ReturnsAsync(card);

        var response = await fixture.Service.CheckAccessAsync(Credential);

        Assert.False(response.Allowed);
        Assert.Equal(StatusCartao.Bloqueado, card.Status);
        var history = Assert.Single(fixture.StatusHistories);
        Assert.Equal(StatusCartao.Bloqueado, history.Status);
        Assert.Equal("Cartão de visitante expirado.", history.Motivo);
        var logged = Assert.Single(fixture.Logs);
        Assert.False(logged.AcessoPermitido);
        Assert.Equal(TipoMotivoAcesso.CartaoExpirado, logged.MotivoTipo);
    }

    [Fact]
    public async Task VisitorCardBeforeExpirationIsAllowed()
    {
        var fixture = new AccessFixture();
        var card = CreateCard(
            StatusCartao.Ativo,
            userActive: true,
            tipo: TipoUsuario.Visitante,
            expiration: DateTime.UtcNow.AddHours(1));
        fixture.CardRepository
            .Setup(repository => repository.GetByUidAsync(Credential))
            .ReturnsAsync(card);

        var response = await fixture.Service.CheckAccessAsync(Credential);

        Assert.True(response.Allowed);
        Assert.Equal(StatusCartao.Ativo, card.Status);
        Assert.Empty(fixture.StatusHistories);
        Assert.True(Assert.Single(fixture.Logs).AcessoPermitido);
    }

    [Fact]
    public async Task BlankLocationUsesConfiguredDefaultInAuditLog()
    {
        var fixture = new AccessFixture();

        await fixture.Service.CheckAccessAsync(Credential, "  ");

        Assert.Equal(LogAcesso.LocalPadrao, Assert.Single(fixture.Logs).Local);
    }

    [Fact]
    public async Task ExpiredDateDoesNotBlockNonVisitorCard()
    {
        var fixture = new AccessFixture();
        var card = CreateCard(
            StatusCartao.Ativo,
            userActive: true,
            tipo: TipoUsuario.Aluno,
            expiration: DateTime.UtcNow.AddSeconds(-1));
        fixture.CardRepository
            .Setup(repository => repository.GetByUidAsync(Credential))
            .ReturnsAsync(card);

        var response = await fixture.Service.CheckAccessAsync(Credential);

        Assert.True(response.Allowed);
        Assert.Equal(StatusCartao.Ativo, card.Status);
        Assert.Empty(fixture.StatusHistories);
        Assert.True(Assert.Single(fixture.Logs).AcessoPermitido);
    }

    private static Cartao CreateCard(
        StatusCartao status,
        bool userActive,
        TipoUsuario tipo = TipoUsuario.Aluno,
        DateTime? expiration = null)
    {
        var user = new Usuario
        {
            Id = UserId,
            Nome = "Pessoa",
            Documento = "document",
            Tipo = tipo,
            IsActive = userActive
        };
        var card = new Cartao
        {
            Id = CardId,
            Uid = Credential,
            UsuarioId = UserId,
            DataExpiracao = expiration
        };
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
        public List<HistoricoStatusCartao> StatusHistories { get; } = [];
        public AcessoService Service { get; }

        public AccessFixture()
        {
            CardRepository.Setup(repository => repository.GetByUidAsync(It.IsAny<string>()))
                .ReturnsAsync((Cartao?)null);
            LogService
                .Setup(service => service.RegistrarTentativaAsync(It.IsAny<CreateLogAcessoDto>()))
                .Callback<CreateLogAcessoDto>(Logs.Add)
                .ReturnsAsync((LogAcessoDto)null!);
            CardRepository
                .Setup(repository => repository.UpdateStatusWithHistoryAsync(
                    It.IsAny<Cartao>(),
                    It.IsAny<HistoricoStatusCartao>()))
                .Callback<Cartao, HistoricoStatusCartao>((_, history) => StatusHistories.Add(history))
                .ReturnsAsync((Cartao card, HistoricoStatusCartao _) => card);
            Service = new AcessoService(
                CardRepository.Object,
                LogService.Object,
                NullLogger<AcessoService>.Instance);
        }
    }
}
