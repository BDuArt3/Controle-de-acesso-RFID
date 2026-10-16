using ControleDeAcesso.Api.Controllers;
using ControleDeAcesso.Application.DTOs;
using ControleDeAcesso.Application.Repositories.Interfaces;
using ControleDeAcesso.Application.Services;
using ControleDeAcesso.Application.Services.Interfaces;
using ControleDeAcesso.Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Text;
using Xunit;

namespace ControleDeAcesso.Tests;

public sealed class LogAcessoExportTests
{
    [Fact]
    public async Task ExportQueryUsesPeriodAndOptionalStatusFilter()
    {
        var start = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 10, 2, 0, 0, 0, DateTimeKind.Utc);
        var repository = new Mock<ILogAcessoRepository>();
        repository
            .Setup(repo => repo.GetByDataPeriodoAndStatusAsync(start, end, false))
            .ReturnsAsync(new[]
            {
                new LogAcesso
                {
                    Id = 42,
                    DataHora = start,
                    UidInformado = "unknown-card",
                    AcessoPermitido = false,
                    Motivo = "Cartão não encontrado"
                }
            });
        var service = CreateService(repository.Object);

        var logs = await service.GetLogsParaExportacaoAsync(start, end, false);

        var log = Assert.Single(logs);
        Assert.Equal(42L, log.Id);
        Assert.Equal("unknown-card", log.CartaoUid);
        repository.Verify(repo => repo.GetByDataPeriodoAndStatusAsync(start, end, false), Times.Once);
        repository.Verify(repo => repo.GetByDataPeriodoAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()), Times.Never);
    }

    [Fact]
    public async Task ExportQueryWithoutStatusIncludesAllResults()
    {
        var start = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddDays(1);
        var repository = new Mock<ILogAcessoRepository>();
        repository.Setup(repo => repo.GetByDataPeriodoAsync(start, end))
            .ReturnsAsync(Array.Empty<LogAcesso>());
        var service = CreateService(repository.Object);

        var logs = await service.GetLogsParaExportacaoAsync(start, end, null);

        Assert.Empty(logs);
        repository.Verify(repo => repo.GetByDataPeriodoAsync(start, end), Times.Once);
    }

    [Fact]
    public async Task ExportQueryAcceptsExactly90Days()
    {
        var start = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddDays(90);
        var repository = new Mock<ILogAcessoRepository>();
        repository.Setup(repo => repo.GetByDataPeriodoAsync(start, end))
            .ReturnsAsync(Array.Empty<LogAcesso>());
        var service = CreateService(repository.Object);

        var logs = await service.GetLogsParaExportacaoAsync(start, end, null);

        Assert.Empty(logs);
        repository.Verify(repo => repo.GetByDataPeriodoAsync(start, end), Times.Once);
    }

    [Theory]
    [InlineData(91)]
    [InlineData(-1)]
    public async Task ExportQueryRejectsPeriodsOverLimitOrReversed(int offsetDays)
    {
        var start = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddDays(offsetDays);
        var service = CreateService(new Mock<ILogAcessoRepository>().Object);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.GetLogsParaExportacaoAsync(start, end, null));
    }

    [Fact]
    public async Task ExportEndpointReturnsCsvDownloadAndEscapesSpreadsheetContent()
    {
        var start = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddDays(1);
        var service = new Mock<ILogAcessoService>();
        service.Setup(logService => logService.GetLogsParaExportacaoAsync(start, end, null))
            .ReturnsAsync(new[]
            {
                new LogAcessoDto
                {
                    Id = 7,
                    DataHora = start,
                    CartaoUid = "card-uid",
                    UsuarioNome = "=2+2;\"Nome\"",
                    AcessoPermitido = false,
                    Motivo = "Falha\r\nna validação"
                }
            });
        var controller = CreateController(service.Object);

        var result = await controller.ExportarCsv(start, end, null);

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("text/csv; charset=utf-8", file.ContentType);
        Assert.Equal("logs-acesso-20261001-20261002.csv", file.FileDownloadName);

        var csv = Encoding.UTF8.GetString(file.FileContents, Encoding.UTF8.GetPreamble().Length,
            file.FileContents.Length - Encoding.UTF8.GetPreamble().Length);
        Assert.StartsWith("\"Id\";\"DataHoraUtc\";\"CartaoUid\"", csv);
        Assert.Contains("\"'=2+2;\"\"Nome\"\"\"", csv);
        Assert.Contains("\"Falha\r\nna validação\"", csv);
        Assert.Contains("\"Negado\"", csv);

        var authorization = typeof(LogAcessoController)
            .GetMethod(nameof(LogAcessoController.ExportarCsv))!
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: false)
            .Cast<AuthorizeAttribute>()
            .Single();
        Assert.Equal("AdminOrOperador", authorization.Policy);
    }

    [Fact]
    public async Task ExportEndpointRequiresBothDates()
    {
        var service = new Mock<ILogAcessoService>();
        var controller = CreateController(service.Object);

        var result = await controller.ExportarCsv(null, DateTime.UtcNow, null);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("dataInicio", Assert.IsType<string>(badRequest.Value));
        service.Verify(
            logService => logService.GetLogsParaExportacaoAsync(
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<bool?>()),
            Times.Never);
    }

    private static LogAcessoService CreateService(ILogAcessoRepository repository) =>
        new(
            repository,
            new Mock<ICartaoRepository>().Object,
            NullLogger<LogAcessoService>.Instance);

    private static LogAcessoController CreateController(ILogAcessoService service) =>
        new(service, NullLogger<LogAcessoController>.Instance);
}
