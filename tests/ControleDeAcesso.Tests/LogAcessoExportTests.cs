using ControleDeAcesso.Api.Controllers;
using ControleDeAcesso.Api.Reports;
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
        Assert.Equal(LogAcesso.LocalPadrao, log.Local);
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
    public async Task RegistrarTentativaAsyncPersistsAndReturnsLocation()
    {
        LogAcesso? persistedLog = null;
        var repository = new Mock<ILogAcessoRepository>();
        repository.Setup(repo => repo.CreateAsync(It.IsAny<LogAcesso>()))
            .ReturnsAsync((LogAcesso log) =>
            {
                persistedLog = log;
                return log;
            });
        var service = CreateService(repository.Object);

        var result = await service.RegistrarTentativaAsync(new CreateLogAcessoDto
        {
            UidInformado = "credential-unknown",
            Local = "  Entrada Bloco A  ",
            AcessoPermitido = false,
            Motivo = "Cartão não encontrado",
            MotivoTipo = TipoMotivoAcesso.CartaoNaoEncontrado
        });

        Assert.NotNull(persistedLog);
        Assert.Equal("Entrada Bloco A", persistedLog.Local);
        Assert.Equal("Entrada Bloco A", result.Local);
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
                    Local = "Entrada Bloco A",
                    UsuarioNome = "=2+2;\"Nome\"",
                    AcessoPermitido = false,
                    Motivo = "Falha\r\nna validação"
                }
            });
        var controller = CreateController(service.Object);

        var result = await controller.Exportar(start, end, null);

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("text/csv; charset=utf-8", file.ContentType);
        Assert.Equal("logs-acesso-20261001-20261002.csv", file.FileDownloadName);

        var csv = Encoding.UTF8.GetString(file.FileContents, Encoding.UTF8.GetPreamble().Length,
            file.FileContents.Length - Encoding.UTF8.GetPreamble().Length);
        Assert.StartsWith("\"Id\";\"DataHoraUtc\";\"CartaoUid\";\"UidInformado\";\"Local\"", csv);
        Assert.Contains("\"Entrada Bloco A\"", csv);
        Assert.Contains("\"'=2+2;\"\"Nome\"\"\"", csv);
        Assert.Contains("\"Falha\r\nna validação\"", csv);
        Assert.Contains("\"Negado\"", csv);

        var authorization = typeof(LogAcessoController)
            .GetMethod(nameof(LogAcessoController.Exportar))!
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: false)
            .Cast<AuthorizeAttribute>()
            .Single();
        Assert.Equal("TodosFuncionarios", authorization.Policy);
    }

    [Fact]
    public async Task ExportEndpointReturnsPdfDownload()
    {
        var start = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddDays(1);
        var service = new Mock<ILogAcessoService>();
        service.Setup(logService => logService.GetLogsParaExportacaoAsync(start, end, false))
            .ReturnsAsync(new[]
            {
                new LogAcessoDto
                {
                    Id = 15,
                    DataHora = start,
                    CartaoUid = "cartao-uid",
                    UidInformado = "uid-informado",
                    UsuarioNome = "Maria Silva",
                    UsuarioTipo = "Funcionário",
                    AcessoPermitido = true,
                    Motivo = "Acesso autorizado"
                }
            });
        var controller = CreateController(service.Object);

        var result = await controller.Exportar(start, end, false, "PDF");

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("application/pdf", file.ContentType);
        Assert.Equal("logs-acesso-20261001-20261002.pdf", file.FileDownloadName);
        Assert.StartsWith("%PDF-", Encoding.ASCII.GetString(file.FileContents, 0, 5));
        service.Verify(
            logService => logService.GetLogsParaExportacaoAsync(start, end, false),
            Times.Once);
    }

    [Fact]
    public async Task ExportEndpointReturnsInformativePdfWhenThereAreNoLogs()
    {
        var start = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddDays(1);
        var service = new Mock<ILogAcessoService>();
        service.Setup(logService => logService.GetLogsParaExportacaoAsync(start, end, null))
            .ReturnsAsync(Array.Empty<LogAcessoDto>());
        var controller = CreateController(service.Object);

        var result = await controller.Exportar(start, end, null, "pdf");

        var file = Assert.IsType<FileContentResult>(result);
        Assert.StartsWith("%PDF-", Encoding.ASCII.GetString(file.FileContents, 0, 5));
        Assert.Equal("application/pdf", file.ContentType);
    }

    [Fact]
    public void PdfRowsUseFriendlyColumnsAndDoNotDuplicateUnknownCardUid()
    {
        var date = new DateTime(2026, 10, 1, 3, 15, 0, DateTimeKind.Utc);
        var rows = LogAcessoPdfGenerator.BuildRows(new[]
        {
            new LogAcessoDto
            {
                DataHora = date,
                CartaoUid = "uid-nao-cadastrado",
                UidInformado = "uid-nao-cadastrado",
                Local = "Entrada Bloco A",
                AcessoPermitido = false,
                Motivo = "Cartão não encontrado"
            },
            new LogAcessoDto
            {
                DataHora = date,
                CartaoUid = "uid-cartao-admin",
                Local = "Portaria Principal",
                AcessoPermitido = true,
                UsuarioNome = "Admin",
                UsuarioTipo = "Colaborador",
                Motivo = "Acesso autorizado"
            }
        });

        Assert.Equal(
            new[]
            {
                "Data e hora (Brasília)",
                "UID do cartão",
                "Local",
                "Usuário",
                "Tipo de usuário",
                "Resultado",
                "Motivo"
            },
            LogAcessoPdfGenerator.ColumnHeaders);
        Assert.Equal(2, rows.Count);
        Assert.Equal("01/10/2026 00:15:00", rows[0].DateTime);
        Assert.Equal("uid-nao-cadastrado", rows[0].CardUid);
        Assert.Equal("Entrada Bloco A", rows[0].Local);
        Assert.Equal("Não identificado", rows[0].UserName);
        Assert.Equal("Não identificado", rows[0].UserType);
        Assert.Equal("Negado", rows[0].Result);
        Assert.Equal("01/10/2026 00:15:00", rows[1].DateTime);
        Assert.Equal("uid-cartao-admin", rows[1].CardUid);
        Assert.Equal("Portaria Principal", rows[1].Local);
        Assert.Equal("Admin", rows[1].UserName);
        Assert.Equal("Colaborador", rows[1].UserType);
        Assert.Equal("Permitido", rows[1].Result);
    }

    [Fact]
    public void PdfSummaryShowsBrasiliaPeriodFilterAndAccessTotals()
    {
        var rows = LogAcessoPdfGenerator.BuildRows(new[]
        {
            new LogAcessoDto { AcessoPermitido = true },
            new LogAcessoDto { AcessoPermitido = true },
            new LogAcessoDto { AcessoPermitido = false }
        });

        var summary = LogAcessoPdfGenerator.BuildSummary(
            rows,
            new DateTime(2026, 10, 1, 3, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 10, 2, 2, 59, 0, DateTimeKind.Utc),
            false);

        Assert.Equal("01/10/2026 00:00 a 01/10/2026 23:59", summary.Period);
        Assert.Equal("Negado", summary.AccessFilter);
        Assert.Equal(3, summary.Total);
        Assert.Equal(2, summary.Allowed);
        Assert.Equal(1, summary.Denied);
    }

    [Fact]
    public void PdfSummaryForEmptyResultsReportsZeroCountsAndAllFilter()
    {
        var summary = LogAcessoPdfGenerator.BuildSummary(
            Array.Empty<LogAcessoPdfRow>(),
            new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 10, 2, 0, 0, 0, DateTimeKind.Utc),
            null);

        Assert.Equal("Todos", summary.AccessFilter);
        Assert.Equal(0, summary.Total);
        Assert.Equal(0, summary.Allowed);
        Assert.Equal(0, summary.Denied);
    }

    [Fact]
    public async Task ExportEndpointRejectsUnknownFormatWithoutQueryingLogs()
    {
        var service = new Mock<ILogAcessoService>();
        var controller = CreateController(service.Object);

        var result = await controller.Exportar(
            DateTime.UtcNow,
            DateTime.UtcNow,
            null,
            "xlsx");

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("Formato inválido", Assert.IsType<string>(badRequest.Value));
        service.Verify(
            logService => logService.GetLogsParaExportacaoAsync(
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<bool?>()),
            Times.Never);
    }

    [Fact]
    public async Task ExportEndpointRequiresBothDates()
    {
        var service = new Mock<ILogAcessoService>();
        var controller = CreateController(service.Object);

        var result = await controller.Exportar(null, DateTime.UtcNow, null);

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
