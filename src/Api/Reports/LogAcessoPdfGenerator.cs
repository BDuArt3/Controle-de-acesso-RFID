using System.Globalization;
using ControleDeAcesso.Application.DTOs;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ControleDeAcesso.Api.Reports;

internal static class LogAcessoPdfGenerator
{
    private static readonly TimeZoneInfo BrasiliaTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

    static LogAcessoPdfGenerator()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    internal static IReadOnlyList<string> ColumnHeaders { get; } =
    [
        "Data e hora (Brasília)",
        "UID do cartão",
        "Usuário",
        "Tipo de usuário",
        "Resultado",
        "Motivo"
    ];

    public static byte[] Generate(
        IEnumerable<LogAcessoDto> logs,
        DateTime dataInicio,
        DateTime dataFim,
        bool? acessoPermitido)
    {
        var rows = BuildRows(logs);
        var summary = BuildSummary(rows, dataInicio, dataFim, acessoPermitido);
        var culture = CultureInfo.GetCultureInfo("pt-BR");

        return Document.Create(document =>
            document.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(24);
                page.DefaultTextStyle(style => style.FontSize(8));

                page.Header().Column(column =>
                {
                    column.Item()
                        .Text("Relatório de acessos")
                        .FontSize(16)
                        .SemiBold();
                    column.Item().Text($"Período (Brasília): {summary.Period}");
                    column.Item().Text($"Filtro de resultado: {summary.AccessFilter}");
                    column.Item().Text(
                        $"Total de registros: {summary.Total.ToString("N0", culture)}  |  " +
                        $"Permitidos: {summary.Allowed.ToString("N0", culture)}  |  " +
                        $"Negados: {summary.Denied.ToString("N0", culture)}");
                });

                page.Content().Column(column =>
                {
                    column.Spacing(6);
                    if (rows.Count == 0)
                    {
                        column.Item()
                            .PaddingTop(8)
                            .Text("Nenhum registro encontrado para os filtros informados.");
                        return;
                    }

                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(1.25f);
                            columns.RelativeColumn(1.1f);
                            columns.RelativeColumn(1.6f);
                            columns.RelativeColumn(1.1f);
                            columns.RelativeColumn(0.9f);
                            columns.RelativeColumn(2.2f);
                        });

                        table.Header(header =>
                        {
                            foreach (var title in ColumnHeaders)
                            {
                                header.Cell()
                                    .Element(HeaderCell)
                                    .Text(title)
                                    .FontSize(8)
                                    .Bold()
                                    .FontColor(Colors.White);
                            }
                        });

                        foreach (var row in rows)
                        {
                            foreach (var value in row.Values)
                            {
                                table.Cell()
                                    .Element(BodyCell)
                                    .Text(value);
                            }
                        }
                    });
                });

                page.Footer()
                    .AlignCenter()
                    .Text(text =>
                    {
                        text.Span("Página ");
                        text.CurrentPageNumber();
                        text.Span(" de ");
                        text.TotalPages();
                    });
            })).GeneratePdf();
    }

    internal static IReadOnlyList<LogAcessoPdfRow> BuildRows(IEnumerable<LogAcessoDto> logs)
    {
        var culture = CultureInfo.GetCultureInfo("pt-BR");
        return logs.Select(log => new LogAcessoPdfRow(
            ConvertFromUtc(log.DataHora).ToString("dd/MM/yyyy HH:mm:ss", culture),
            GetCardUid(log),
            GetValueOrFallback(log.UsuarioNome, "Não identificado"),
            GetValueOrFallback(log.UsuarioTipo, "Não identificado"),
            log.AcessoPermitido ? "Permitido" : "Negado",
            GetValueOrFallback(log.Motivo, "Não informado"))).ToArray();
    }

    internal static LogAcessoPdfSummary BuildSummary(
        IReadOnlyCollection<LogAcessoPdfRow> rows,
        DateTime dataInicio,
        DateTime dataFim,
        bool? acessoPermitido)
    {
        var culture = CultureInfo.GetCultureInfo("pt-BR");
        var startLocal = ConvertFromUtc(ToUtc(dataInicio));
        var endLocal = ConvertFromUtc(ToUtc(dataFim));

        return new LogAcessoPdfSummary(
            $"{startLocal.ToString("dd/MM/yyyy HH:mm", culture)} a " +
            endLocal.ToString("dd/MM/yyyy HH:mm", culture),
            GetAccessFilterLabel(acessoPermitido),
            rows.Count,
            rows.Count(row => row.Result == "Permitido"),
            rows.Count(row => row.Result == "Negado"));
    }

    private static string GetCardUid(LogAcessoDto log)
    {
        if (!string.IsNullOrWhiteSpace(log.CartaoUid))
        {
            return log.CartaoUid;
        }

        return GetValueOrFallback(log.UidInformado, "Não informado");
    }

    private static string GetValueOrFallback(string? value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value;

    private static string GetAccessFilterLabel(bool? acessoPermitido) => acessoPermitido switch
    {
        true => "Permitido",
        false => "Negado",
        null => "Todos"
    };

    private static DateTime ConvertFromUtc(DateTime value) =>
        TimeZoneInfo.ConvertTimeFromUtc(ToUtc(value), BrasiliaTimeZone);

    private static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };

    private static IContainer HeaderCell(IContainer container) =>
        container.Background(Colors.Grey.Darken2)
            .PaddingVertical(5)
            .PaddingHorizontal(4);

    private static IContainer BodyCell(IContainer container) =>
        container.BorderBottom(0.5f)
            .BorderColor(Colors.Grey.Lighten2)
            .PaddingVertical(4)
            .PaddingHorizontal(4);
}

internal sealed record LogAcessoPdfRow(
    string DateTime,
    string CardUid,
    string UserName,
    string UserType,
    string Result,
    string Reason)
{
    public IEnumerable<string> Values =>
    [
        DateTime,
        CardUid,
        UserName,
        UserType,
        Result,
        Reason
    ];
}

internal sealed record LogAcessoPdfSummary(
    string Period,
    string AccessFilter,
    int Total,
    int Allowed,
    int Denied);
