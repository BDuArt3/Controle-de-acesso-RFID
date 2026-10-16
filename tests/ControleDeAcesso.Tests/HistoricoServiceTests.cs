using ControleDeAcesso.Application.DTOs;
using ControleDeAcesso.Application.Repositories.Interfaces;
using ControleDeAcesso.Application.Services;
using Moq;
using Xunit;

namespace ControleDeAcesso.Tests;

public sealed class HistoricoServiceTests
{
    [Fact]
    public async Task PaginationIsNormalizedBeforeQueryingRepository()
    {
        var repository = new Mock<IHistoricoRepository>();
        repository.Setup(r => r.GetStatusUsuarioAsync("user", 1, HistoricoService.MaxPageSize))
            .ReturnsAsync(new PagedResultDto<HistoricoStatusAtivoDto>());
        var service = new HistoricoService(repository.Object);

        var result = await service.GetUsuarioAsync("user", -5, 10_000);

        Assert.NotNull(result);
        repository.Verify(r => r.GetStatusUsuarioAsync("user", 1, HistoricoService.MaxPageSize), Times.Once);
    }

    [Fact]
    public async Task InvalidPageSizeFallsBackToDefault()
    {
        var repository = new Mock<IHistoricoRepository>();
        var service = new HistoricoService(repository.Object);

        await service.GetFuncionarioAsync("employee", 2, 0);

        repository.Verify(r => r.GetStatusFuncionarioAsync("employee", 2, HistoricoService.DefaultPageSize), Times.Once);
    }

    [Fact]
    public async Task MissingEntityReturnsNull()
    {
        var repository = new Mock<IHistoricoRepository>();
        repository.Setup(r => r.GetCartaoAsync("card", 1, 20)).ReturnsAsync((HistoricoCartaoDto?)null);
        var service = new HistoricoService(repository.Object);

        Assert.Null(await service.GetCartaoAsync("card", 1, 20));
    }

    [Fact]
    public async Task BlankIdIsRejected()
    {
        var service = new HistoricoService(Mock.Of<IHistoricoRepository>());

        await Assert.ThrowsAsync<ArgumentException>(() => service.GetCartaoAsync(" ", 1, 20));
    }
}
