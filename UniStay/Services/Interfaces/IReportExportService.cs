using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace UniStay.Services.Interfaces;

public interface IReportExportService
{
    byte[] ExportToExcel<T>(string title, string[] columns, IEnumerable<T> rows, Func<T, object?[]> cellSelector);
    byte[] ExportToPdf(string title, string[] columns, string[][] rows);
}
