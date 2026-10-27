using System.Globalization;
using ControleDeAcesso.Application.DTOs;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ControleDeAcesso.Api.Reports;

internal static class LogAcessoPdfGenerator
{
    static LogAcessoPdfGenerator()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    private static readonly string[] Headers =
    [
        "Id",
        "DataHoraUtc",
        "CartaoUid",
        "UidInformado",
        "UsuarioNome",
        "UsuarioTipo",
        "Resultado",
        "Motivo",
        "MotivoTipo"
    ];

    public static byte[] Generate(
        IEnumerable<LogAcessoDto> logs,
        DateTime dataInicio,
        DateTime dataFim,
        bool? acessoPermitido)
    {
        var entries = logs.ToArray();
        var culture = CultureInfo.GetCultureInfo("pt-BR");

        return Document.Create(document =>
            document.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(24);
                page.DefaultTextStyle(style => style.FontSize(7));

                page.Header().Column(column =>
                {
                    column.Item()
                        .Text("Relatório de logs de acesso")
                        .FontSize(16)
                        .SemiBold();
                    column.Item().Text(
                        $"Período solicitado: {dataInicio.ToString("dd/MM/yyyy HH:mm", culture)} a " +
                        $"{dataFim.ToString("dd/MM/yyyy HH:mm", culture)}");
                    column.Item().Text($"Acesso: {GetAccessFilterLabel(acessoPermitido)}");
                });

                page.Content().Column(column =>
                {
                    column.Spacing(6);
                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(0.6f);
                            columns.RelativeColumn(1.2f);
                            columns.RelativeColumn(1f);
                            columns.RelativeColumn(1f);
                            columns.RelativeColumn(1.4f);
                            columns.RelativeColumn(0.8f);
                            columns.RelativeColumn(0.8f);
                            columns.RelativeColumn(2f);
                            columns.RelativeColumn(1.2f);
                        });

                        table.Header(header =>
                        {
                            foreach (var title in Headers)
                            {
                                header.Cell()
                                    .Element(HeaderCell)
                                    .Text(title)
                                    .FontSize(7)
                                    .Bold()
                                    .FontColor(Colors.White);
                            }
                        });

                        foreach (var log in entries)
                        {
                            foreach (var value in GetRowValues(log))
                            {
                                table.Cell().Element(BodyCell).Text(value);
                            }
                        }
                    });

                    if (entries.Length == 0)
                    {
                        column.Item()
                            .PaddingTop(8)
                            .Text("Nenhum registro encontrado para os filtros informados.");
                    }
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

    private static string GetAccessFilterLabel(bool? acessoPermitido) => acessoPermitido switch
    {
        true => "Permitido",
        false => "Negado",
        null => "Todos"
    };

    private static IEnumerable<string> GetRowValues(LogAcessoDto log)
    {
        yield return log.Id.ToString(CultureInfo.InvariantCulture);
        yield return log.DataHora.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture);
        yield return log.CartaoUid;
        yield return log.UidInformado ?? string.Empty;
        yield return log.UsuarioNome ?? string.Empty;
        yield return log.UsuarioTipo ?? string.Empty;
        yield return log.AcessoPermitido ? "Permitido" : "Negado";
        yield return log.Motivo;
        yield return log.MotivoTipo.ToString();
    }

    private static IContainer HeaderCell(IContainer container) =>
        container.Background(Colors.Grey.Darken2)
            .PaddingVertical(4)
            .PaddingHorizontal(3);

    private static IContainer BodyCell(IContainer container) =>
        container.BorderBottom(0.5f)
            .BorderColor(Colors.Grey.Lighten2)
            .PaddingVertical(3)
            .PaddingHorizontal(3);
}
