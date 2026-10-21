using ControleDeAcesso.Application.DTOs;
using ControleDeAcesso.Application.Repositories.Interfaces;
using ControleDeAcesso.Application.Services;
using ControleDeAcesso.Core.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace ControleDeAcesso.Tests;

public sealed class CardIssuanceTests
{
    private const string RequesterId = "requester-id";
    private const string UserId = "user-id";

    [Fact]
    public async Task NewCardIsCreatedWithAuditOnlyAfterHardwareConfirmation()
    {
        var fixture = new CardServiceFixture();
        var issued = await fixture.Service.SolicitarGravacaoAsync(
            new GravarCartaoDto { UsuarioId = UserId },
            RequesterId,
            "request-1",
            CancellationToken.None);

        Assert.Equal("Pendente", issued.Response.Status);
        Assert.NotNull(issued.Command);
        Assert.Empty(fixture.CreatedCards);
        Assert.Equal(RequesterId, fixture.Requests.Single().SolicitanteFuncionarioId);

        await fixture.ConfirmAsync(issued.Command!);

        var (card, association, initialStatus) = Assert.Single(fixture.CreatedCards);
        Assert.Equal(UserId, card.UsuarioId);
        Assert.Equal(CredentialLength, card.Uid.Length);
        Assert.Equal(StatusCartao.Ativo, initialStatus.Status);
        Assert.Equal(card.Id, association.CartaoId);
        Assert.Equal(RequesterId, association.AssociadoPorFuncionarioId);
        Assert.Equal(RequesterId, initialStatus.AlteradoPorFuncionarioId);
        Assert.Equal(StatusSolicitacaoGravacao.Concluida, fixture.Requests.Single().Status);
    }

    [Fact]
    public async Task VisitorCardExpires24HoursAfterIssuanceConfirmation()
    {
        var fixture = new CardServiceFixture { UserType = TipoUsuario.Visitante };
        var issued = await fixture.RequestNewCardAsync();

        await fixture.ConfirmAsync(issued.Command!);

        var (card, _, _) = Assert.Single(fixture.CreatedCards);
        Assert.NotNull(card.DataExpiracao);
        Assert.Equal(TimeSpan.FromHours(24), card.DataExpiracao!.Value - card.DataRegistro);
    }

    [Fact]
    public async Task RepeatedConfirmationDoesNotCreateAnotherLogicalCard()
    {
        var fixture = new CardServiceFixture();
        var issued = await fixture.RequestNewCardAsync();
        await fixture.ConfirmAsync(issued.Command!);

        var repeated = await fixture.ConfirmAsync(issued.Command!);

        Assert.Equal("Concluida", repeated!.Status);
        Assert.Single(fixture.CreatedCards);
    }

    [Fact]
    public async Task ConfirmationRejectsNonHexadecimalCredential()
    {
        var fixture = new CardServiceFixture();
        var issued = await fixture.RequestNewCardAsync();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            fixture.Service.ConfirmarGravacaoAsync(
                new ConfirmarGravacaoCartaoDto
                {
                    SolicitacaoId = issued.Response.SolicitacaoId,
                    CredencialGravada = "ZZZZZZZZZZZZZZZZ"
                },
                CancellationToken.None));

        Assert.Empty(fixture.CreatedCards);
    }

    [Fact]
    public async Task DuplicateCredentialOwnedByAnotherCardIsRejected()
    {
        var fixture = new CardServiceFixture();
        var issued = await fixture.RequestNewCardAsync();
        var collision = new Cartao { Uid = issued.Command!.Credencial, UsuarioId = "other-user" };
        fixture.Cartoes
            .Setup(repository => repository.GetByUidAsync(issued.Command.Credencial))
            .ReturnsAsync(collision);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fixture.ConfirmAsync(issued.Command!));

        Assert.Empty(fixture.CreatedCards);
        Assert.Equal(StatusSolicitacaoGravacao.Pendente, fixture.Requests.Single().Status);
    }

    [Fact]
    public async Task IssuingAnotherCardDoesNotDeactivateExistingCardsForSameUser()
    {
        var fixture = new CardServiceFixture();
        var existing = new Cartao { Uid = "1111111111111111", UsuarioId = UserId };
        var issued = await fixture.RequestNewCardAsync();

        await fixture.ConfirmAsync(issued.Command!);

        Assert.Equal(StatusCartao.Ativo, existing.Status);
        fixture.Cartoes.Verify(
            repository => repository.UpdateStatusWithHistoryAsync(
                It.IsAny<Cartao>(),
                It.IsAny<HistoricoStatusCartao>()),
            Times.Never);
    }

    [Fact]
    public async Task ReissueCancelsOldIssuanceAndCreatesNewCardAfterConfirmation()
    {
        var fixture = new CardServiceFixture();
        var oldCard = new Cartao { Uid = "1111111111111111", UsuarioId = UserId };
        var oldAssociation = new HistoricoAssociacoesCartao
        {
            CartaoId = oldCard.Id,
            DataAssociacao = DateTime.UtcNow.AddDays(-5)
        };
        oldCard.HistoricoAssociacaoCartao = oldAssociation;
        fixture.Cartoes
            .Setup(repository => repository.GetByIdAsync(oldCard.Id))
            .ReturnsAsync(oldCard);

        var issued = await fixture.Service.SolicitarGravacaoAsync(
            new GravarCartaoDto
            {
                UsuarioId = UserId,
                CartaoAnteriorId = oldCard.Id,
                MotivoReemissao = "Plástico repassado"
            },
            RequesterId,
            "reissue-1",
            CancellationToken.None);

        Assert.Equal(StatusCartao.Bloqueado, oldCard.Status);
        Assert.Empty(fixture.CreatedCards);

        await fixture.ConfirmAsync(issued.Command!);

        Assert.Equal(StatusCartao.Cancelado, oldCard.Status);
        Assert.NotNull(oldAssociation.DataDesassociacao);
        Assert.Equal(RequesterId, oldAssociation.DesassociadoPorFuncionarioId);
        Assert.Equal(StatusCartao.Ativo, Assert.Single(fixture.CreatedCards).Card.Status);
    }

    [Fact]
    public async Task FailedReissueKeepsOldCardBlockedUntilExplicitRetry()
    {
        var fixture = new CardServiceFixture();
        var oldCard = new Cartao { Uid = "1111111111111111", UsuarioId = UserId };
        oldCard.HistoricoAssociacaoCartao = new HistoricoAssociacoesCartao
        {
            CartaoId = oldCard.Id,
            DataAssociacao = DateTime.UtcNow.AddDays(-1)
        };
        fixture.Cartoes.Setup(repository => repository.GetByIdAsync(oldCard.Id)).ReturnsAsync(oldCard);

        var issued = await fixture.Service.SolicitarGravacaoAsync(
            new GravarCartaoDto
            {
                UsuarioId = UserId,
                CartaoAnteriorId = oldCard.Id,
                MotivoReemissao = "Reemissão"
            },
            RequesterId,
            "reissue-fail",
            CancellationToken.None);

        await fixture.Service.RegistrarFalhaGravacaoAsync(
            new FalharGravacaoCartaoDto
            {
                SolicitacaoId = issued.Response.SolicitacaoId,
                Motivo = "Falha no hardware"
            },
            CancellationToken.None);

        Assert.Equal(StatusCartao.Bloqueado, oldCard.Status);
        Assert.Empty(fixture.CreatedCards);

        var retry = await fixture.Service.PrepararRetentativaAsync(
            issued.Response.SolicitacaoId,
            CancellationToken.None);

        Assert.NotNull(retry);
        Assert.Equal(issued.Command!.Credencial, retry.Credencial);
        Assert.Equal(StatusCartao.Bloqueado, oldCard.Status);
        Assert.Empty(fixture.CreatedCards);
    }

    [Fact]
    public async Task IdempotencyKeyCannotBeReusedWithDifferentRecipient()
    {
        var fixture = new CardServiceFixture();
        await fixture.Service.SolicitarGravacaoAsync(
            new GravarCartaoDto { UsuarioId = UserId },
            RequesterId,
            "same-key",
            CancellationToken.None);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fixture.Service.SolicitarGravacaoAsync(
                new GravarCartaoDto { UsuarioId = "another-user" },
                RequesterId,
                "same-key",
                CancellationToken.None));
    }

    private const int CredentialLength = 16;

    private sealed class CardServiceFixture
    {
        public Mock<ICartaoRepository> Cartoes { get; } = new();
        public List<SolicitacaoGravacaoCartao> Requests { get; } = [];
        public List<(Cartao Card, HistoricoAssociacoesCartao Association, HistoricoStatusCartao Status)> CreatedCards { get; } = [];
        public TipoUsuario UserType { get; set; } = TipoUsuario.Aluno;
        public CartaoService Service { get; }

        public CardServiceFixture()
        {
            var usuarioRepository = new Mock<IUsuarioRepository>();
            usuarioRepository
                .Setup(repository => repository.GetByIdAsync(It.IsAny<string>()))
                .ReturnsAsync((string id) => new Usuario
                {
                    Id = id,
                    Nome = "Titular",
                    Documento = "doc",
                    Tipo = UserType,
                    IsActive = true
                });

            var funcionarioRepository = new Mock<IFuncionarioRepository>();
            funcionarioRepository
                .Setup(repository => repository.GetByIdAsync(RequesterId))
                .ReturnsAsync(new Funcionario { Id = RequesterId, IsActive = true });

            var requestRepository = new Mock<ISolicitacaoGravacaoCartaoRepository>();
            requestRepository
                .Setup(repository => repository.GetByIdempotencyKeyAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((string requesterId, string key, CancellationToken _) =>
                    Requests.SingleOrDefault(request =>
                        request.SolicitanteFuncionarioId == requesterId &&
                        request.ChaveIdempotencia == key));
            requestRepository
                .Setup(repository => repository.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((string id, CancellationToken _) =>
                    Requests.SingleOrDefault(request => request.Id == id));
            requestRepository
                .Setup(repository => repository.CredentialExistsAsync(
                    It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            requestRepository
                .Setup(repository => repository.HasOpenRequestForCardAsync(
                    It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            requestRepository
                .Setup(repository => repository.AddAsync(
                    It.IsAny<SolicitacaoGravacaoCartao>(), It.IsAny<CancellationToken>()))
                .Callback<SolicitacaoGravacaoCartao, CancellationToken>((request, _) => Requests.Add(request))
                .Returns(Task.CompletedTask);

            Cartoes.Setup(repository => repository.GetByUidAsync(It.IsAny<string>()))
                .ReturnsAsync((Cartao?)null);
            Cartoes.Setup(repository => repository.CreateWithAuditAsync(
                    It.IsAny<Cartao>(),
                    It.IsAny<HistoricoAssociacoesCartao>(),
                    It.IsAny<HistoricoStatusCartao>()))
                .Callback<Cartao, HistoricoAssociacoesCartao, HistoricoStatusCartao>(
                    (card, association, status) => CreatedCards.Add((card, association, status)))
                .ReturnsAsync((Cartao card, HistoricoAssociacoesCartao _, HistoricoStatusCartao _) => card);

            var unitOfWork = new Mock<IUnitOfWork>();
            unitOfWork
                .Setup(work => work.ExecuteInTransactionAsync(It.IsAny<Func<Task<GravacaoSolicitadaResult>>>()))
                .Returns((Func<Task<GravacaoSolicitadaResult>> operation) => operation());
            unitOfWork
                .Setup(work => work.ExecuteInTransactionAsync(It.IsAny<Func<Task<GravacaoCartaoResponseDto?>>>()))
                .Returns((Func<Task<GravacaoCartaoResponseDto?>> operation) => operation());
            unitOfWork
                .Setup(work => work.ExecuteInTransactionAsync(It.IsAny<Func<Task<GravacaoCartaoCommandDto?>>>()))
                .Returns((Func<Task<GravacaoCartaoCommandDto?>> operation) => operation());
            unitOfWork
                .Setup(work => work.ExecuteInTransactionAsync(It.IsAny<Func<Task<bool>>>()))
                .Returns((Func<Task<bool>> operation) => operation());

            requestRepository
                .Setup(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            Service = new CartaoService(
                Cartoes.Object,
                usuarioRepository.Object,
                funcionarioRepository.Object,
                requestRepository.Object,
                unitOfWork.Object,
                NullLogger<CartaoService>.Instance);
        }

        public Task<GravacaoSolicitadaResult> RequestNewCardAsync() =>
            Service.SolicitarGravacaoAsync(
                new GravarCartaoDto { UsuarioId = UserId },
                RequesterId,
                Guid.NewGuid().ToString(),
                CancellationToken.None);

        public Task<GravacaoCartaoResponseDto?> ConfirmAsync(GravacaoCartaoCommandDto command) =>
            Service.ConfirmarGravacaoAsync(
                new ConfirmarGravacaoCartaoDto
                {
                    SolicitacaoId = command.SolicitacaoId,
                    CredencialGravada = command.Credencial
                },
                CancellationToken.None);
    }
}
