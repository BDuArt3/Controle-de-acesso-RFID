using ControleDeAcesso.Application.Repositories.Interfaces;
using ControleDeAcesso.Application.Services;
using ControleDeAcesso.Core.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace ControleDeAcesso.Tests;

public sealed class StatusTransitionTests
{
    [Fact]
    public async Task RepeatedCardStatusRequestDoesNotAddAnotherHistoryEntry()
    {
        var cardRepository = new Mock<ICartaoRepository>();
        var card = new Cartao { Id = "card", Uid = "0123456789ABCDEF", UsuarioId = "user" };
        card.AlterarStatus(StatusCartao.Bloqueado);
        cardRepository.Setup(repository => repository.GetByIdAsync(card.Id)).ReturnsAsync(card);
        var service = CreateCardService(cardRepository.Object);

        var changed = await service.BlockAsync(card.Id, "operator", "Motivo");

        Assert.True(changed);
        cardRepository.Verify(
            repository => repository.UpdateStatusWithHistoryAsync(
                It.IsAny<Cartao>(),
                It.IsAny<HistoricoStatusCartao>()),
            Times.Never);
    }

    [Fact]
    public async Task CancelledCardCannotBeReactivated()
    {
        var cardRepository = new Mock<ICartaoRepository>();
        var card = new Cartao { Id = "card", Uid = "0123456789ABCDEF", UsuarioId = "user" };
        card.AlterarStatus(StatusCartao.Cancelado);
        cardRepository.Setup(repository => repository.GetByIdAsync(card.Id)).ReturnsAsync(card);
        var service = CreateCardService(cardRepository.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UnblockAsync(card.Id, "operator", "Tentativa de reativação"));
    }

    [Fact]
    public async Task UnblockAndCancelTransitionsRecordActorAndCloseAssociation()
    {
        var cardRepository = new Mock<ICartaoRepository>();
        var card = new Cartao { Id = "card", Uid = "0123456789ABCDEF", UsuarioId = "user" };
        card.AlterarStatus(StatusCartao.Bloqueado);
        card.HistoricoAssociacaoCartao = new HistoricoAssociacoesCartao
        {
            CartaoId = card.Id,
            DataAssociacao = DateTime.UtcNow.AddDays(-1)
        };
        typeof(Cartao).GetProperty(nameof(Cartao.Usuario))!.SetValue(
            card,
            new Usuario { Id = "user", Nome = "Titular", Documento = "document", IsActive = true });
        cardRepository.Setup(repository => repository.GetByIdAsync(card.Id)).ReturnsAsync(card);
        cardRepository.Setup(repository => repository.GetByUsuarioIdAsync(card.UsuarioId))
            .ReturnsAsync(Array.Empty<Cartao>());
        cardRepository.Setup(repository => repository.UpdateStatusWithHistoryAsync(
                It.IsAny<Cartao>(), It.IsAny<HistoricoStatusCartao>()))
            .ReturnsAsync((Cartao updatedCard, HistoricoStatusCartao _) => updatedCard);
        var service = CreateCardService(cardRepository.Object);

        Assert.True(await service.UnblockAsync(card.Id, "operator", "Desbloqueio aprovado"));
        Assert.True(await service.CancelAsync(card.Id, "operator", "Substituição do cartão"));

        Assert.Equal(StatusCartao.Cancelado, card.Status);
        Assert.NotNull(card.HistoricoAssociacaoCartao.DataDesassociacao);
        Assert.Equal("operator", card.HistoricoAssociacaoCartao.DesassociadoPorFuncionarioId);
        cardRepository.Verify(
            repository => repository.UpdateStatusWithHistoryAsync(
                card,
                It.Is<HistoricoStatusCartao>(history =>
                    history.AlteradoPorFuncionarioId == "operator" &&
                    (history.Motivo == "Desbloqueio aprovado" ||
                     history.Motivo == "Substituição do cartão"))),
            Times.Exactly(2));
    }

    [Fact]
    public async Task UnblockIsRejectedWhenUserAlreadyHasAnotherActiveCard()
    {
        var cardRepository = new Mock<ICartaoRepository>();
        var blocked = new Cartao { Id = "blocked", Uid = "1111111111111111", UsuarioId = "user" };
        blocked.AlterarStatus(StatusCartao.Bloqueado);
        typeof(Cartao).GetProperty(nameof(Cartao.Usuario))!.SetValue(
            blocked,
            new Usuario { Id = "user", Nome = "Titular", Documento = "document", IsActive = true });
        cardRepository.Setup(repository => repository.GetByIdAsync(blocked.Id)).ReturnsAsync(blocked);
        cardRepository.Setup(repository => repository.GetByUsuarioIdAsync(blocked.UsuarioId))
            .ReturnsAsync(new[] { new Cartao { Id = "active", Uid = "2222222222222222", UsuarioId = "user" } });
        var service = CreateCardService(cardRepository.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UnblockAsync(blocked.Id, "operator", "Desbloqueio aprovado"));

        Assert.Equal(StatusCartao.Bloqueado, blocked.Status);
        cardRepository.Verify(
            repository => repository.UpdateStatusWithHistoryAsync(
                It.IsAny<Cartao>(),
                It.IsAny<HistoricoStatusCartao>()),
            Times.Never);
    }

    [Fact]
    public async Task EmployeeOffboardingDeactivatesAccountAndBlocksEveryActiveCard()
    {
        var user = new Usuario
        {
            Id = "user",
            Nome = "Funcionário",
            Documento = "document",
            IsActive = true
        };
        var employee = new Funcionario
        {
            Id = "employee",
            UsuarioId = user.Id,
            Usuario = user,
            IsActive = true
        };
        var first = new Cartao { Id = "card-1", Uid = "1111111111111111", UsuarioId = user.Id };
        var second = new Cartao { Id = "card-2", Uid = "2222222222222222", UsuarioId = user.Id };
        var alreadyBlocked = new Cartao { Id = "card-3", Uid = "3333333333333333", UsuarioId = user.Id };
        alreadyBlocked.AlterarStatus(StatusCartao.Bloqueado);

        var employeeRepository = new Mock<IFuncionarioRepository>();
        employeeRepository.Setup(repository => repository.GetByIdAsync(employee.Id)).ReturnsAsync(employee);
        employeeRepository.Setup(repository => repository.UpdateAsync(employee)).ReturnsAsync(employee);
        var cardRepository = new Mock<ICartaoRepository>();
        cardRepository.Setup(repository => repository.GetAllByUsuarioIdAsync(user.Id))
            .ReturnsAsync(new[] { first, second, alreadyBlocked });
        var service = CreateEmployeeService(employeeRepository.Object, cardRepository.Object);

        var changed = await service.DeleteAsync(employee.Id, "admin", "Desligamento");

        Assert.True(changed);
        Assert.False(employee.IsActive);
        Assert.False(user.IsActive);
        Assert.Equal(StatusCartao.Bloqueado, first.Status);
        Assert.Equal(StatusCartao.Bloqueado, second.Status);
        Assert.Equal(StatusCartao.Bloqueado, alreadyBlocked.Status);
        cardRepository.Verify(
            repository => repository.UpdateStatusWithHistoryAsync(
                It.IsAny<Cartao>(),
                It.Is<HistoricoStatusCartao>(history =>
                    history.Status == StatusCartao.Bloqueado &&
                    history.AlteradoPorFuncionarioId == "admin" &&
                    history.Motivo == "Desligamento")),
            Times.Exactly(2));
    }

    [Fact]
    public async Task ReactivatingEmployeeLeavesCardsBlocked()
    {
        var user = new Usuario
        {
            Id = "user",
            Nome = "Funcionário",
            Documento = "document",
            IsActive = false
        };
        var employee = new Funcionario
        {
            Id = "employee",
            UsuarioId = user.Id,
            Usuario = user,
            IsActive = false
        };
        var blockedCard = new Cartao { Id = "card", Uid = "1111111111111111", UsuarioId = user.Id };
        blockedCard.AlterarStatus(StatusCartao.Bloqueado);
        var employeeRepository = new Mock<IFuncionarioRepository>();
        employeeRepository.Setup(repository => repository.GetByIdAsync(employee.Id)).ReturnsAsync(employee);
        employeeRepository.Setup(repository => repository.UpdateAsync(employee)).ReturnsAsync(employee);
        var cards = new Mock<ICartaoRepository>();
        cards.Setup(repository => repository.GetAllByUsuarioIdAsync(user.Id))
            .ReturnsAsync(new[] { blockedCard });
        var service = CreateEmployeeService(employeeRepository.Object, cards.Object);

        await service.ActivateAsync(employee.Id, "admin", "Retorno");

        Assert.True(employee.IsActive);
        Assert.True(user.IsActive);
        Assert.Equal(StatusCartao.Bloqueado, blockedCard.Status);
        cards.Verify(
            repository => repository.UpdateStatusWithHistoryAsync(
                It.IsAny<Cartao>(),
                It.IsAny<HistoricoStatusCartao>()),
            Times.Never);
    }

    private static CartaoService CreateCardService(ICartaoRepository cardRepository)
    {
        var userRepository = new Mock<IUsuarioRepository>();
        var employeeRepository = new Mock<IFuncionarioRepository>();
        var requestRepository = new Mock<ISolicitacaoGravacaoCartaoRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork
            .Setup(work => work.ExecuteInTransactionAsync(It.IsAny<Func<Task<bool>>>()))
            .Returns((Func<Task<bool>> operation) => operation());
        return new CartaoService(
            cardRepository,
            userRepository.Object,
            employeeRepository.Object,
            requestRepository.Object,
            unitOfWork.Object,
            NullLogger<CartaoService>.Instance);
    }

    private static FuncionarioService CreateEmployeeService(
        IFuncionarioRepository employeeRepository,
        ICartaoRepository cardRepository)
    {
        var userRepository = new Mock<IUsuarioRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork
            .Setup(work => work.ExecuteInTransactionAsync(It.IsAny<Func<Task<bool>>>()))
            .Returns((Func<Task<bool>> operation) => operation());
        return new FuncionarioService(
            employeeRepository,
            userRepository.Object,
            cardRepository,
            unitOfWork.Object,
            NullLogger<FuncionarioService>.Instance);
    }
}
