using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using UniStay.Services.Interfaces;

namespace UniStay.Services.Implementations;

public class ReportExportService : IReportExportService
{
    public byte[] ExportToExcel<T>(string title, string[] columns, IEnumerable<T> rows, Func<T, object?[]> cellSelector)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add(title);

        ws.Cell(1, 1).Value = title;
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 14;
        ws.Range(1, 1, 1, columns.Length).Merge();

        for (int c = 0; c < columns.Length; c++)
        {
            ws.Cell(2, c + 1).Value = columns[c];
            ws.Cell(2, c + 1).Style.Font.Bold = true;
            ws.Cell(2, c + 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        int row = 3;
        foreach (var item in rows)
        {
            var values = cellSelector(item);
            for (int c = 0; c < values.Length && c < columns.Length; c++)
            {
                var val = values[c];
                if (val != null)
                    ws.Cell(row, c + 1).Value = XLHelper.GetTypedValue(val);
                else
                    ws.Cell(row, c + 1).Value = "";
            }
            row++;
        }

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    public byte[] ExportToPdf(string title, string[] columns, string[][] rows)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(20);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Element(c => c
                    .PaddingBottom(10)
                    .Text(title)
                    .SemiBold().FontSize(14));

                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        for (int i = 0; i < columns.Length; i++)
                            c.RelativeColumn();
                    });

                    // Header
                    foreach (var col in columns)
                    {
                        table.Cell().Element(cellStyle).Text(col).SemiBold();
                    }

                    // Rows
                    foreach (var row in rows)
                    {
                        foreach (var cell in row)
                        {
                            table.Cell().Element(cellStyle).Text(cell ?? "");
                        }
                    }
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.CurrentPageNumber();
                    text.Span(" / ");
                    text.TotalPages();
                });
            });
        }).GeneratePdf();
    }

    private static QuestPDF.Infrastructure.IContainer cellStyle(QuestPDF.Infrastructure.IContainer container)
    {
        return container.Padding(2).Border(1).BorderColor(Colors.Grey.Lighten2);
    }
}

    internal static class XLHelper
{
    public static XLCellValue GetTypedValue(object value)
    {
        return value switch
        {
            int i => i,
            long l => l,
            decimal d => d,
            double dbl => dbl,
            float f => f,
            bool b => b,
            DateTime dt => dt,
            string s => s,
            _ => value.ToString() ?? ""
        };
    }
}
