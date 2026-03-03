using ClosedXML.Excel;

namespace DevInstance.DevCoreApp.Server.Admin.Services.ImportExport.Parsing;

public class ExcelFileParser : IFileParser
{
    public Task<List<string>> ParseHeadersAsync(Stream stream)
    {
        stream.Position = 0;
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheets.First();

        var headers = new List<string>();
        var lastColumn = worksheet.LastColumnUsed()?.ColumnNumber() ?? 0;

        for (int col = 1; col <= lastColumn; col++)
        {
            headers.Add(worksheet.Cell(1, col).GetString());
        }

        return Task.FromResult(headers);
    }

    public Task<List<string[]>> ParseRowsAsync(Stream stream)
    {
        stream.Position = 0;
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheets.First();

        var lastColumn = worksheet.LastColumnUsed()?.ColumnNumber() ?? 0;
        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;

        var rows = new List<string[]>();
        for (int row = 2; row <= lastRow; row++)
        {
            var values = new string[lastColumn];
            for (int col = 1; col <= lastColumn; col++)
            {
                values[col - 1] = worksheet.Cell(row, col).GetString();
            }
            rows.Add(values);
        }

        return Task.FromResult(rows);
    }
}
