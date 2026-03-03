using ClosedXML.Excel;

namespace DevInstance.DevCoreApp.Server.Admin.Services.ImportExport.Generation;

public class ExcelFileGenerator : IFileGenerator
{
    public Task<Stream> GenerateAsync(List<string> headers, List<Dictionary<string, string?>> rows)
    {
        var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Export");

        for (int col = 0; col < headers.Count; col++)
        {
            worksheet.Cell(1, col + 1).Value = headers[col];
            worksheet.Cell(1, col + 1).Style.Font.Bold = true;
        }

        for (int row = 0; row < rows.Count; row++)
        {
            for (int col = 0; col < headers.Count; col++)
            {
                rows[row].TryGetValue(headers[col], out var value);
                worksheet.Cell(row + 2, col + 1).Value = value ?? string.Empty;
            }
        }

        worksheet.Columns().AdjustToContents();

        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        return Task.FromResult<Stream>(stream);
    }
}
