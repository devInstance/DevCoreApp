using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;

namespace DevInstance.DevCoreApp.Server.Admin.Services.ImportExport.Parsing;

public class CsvFileParser : IFileParser
{
    public Task<List<string>> ParseHeadersAsync(Stream stream)
    {
        stream.Position = 0;
        using var reader = new StreamReader(stream, leaveOpen: true);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true
        });

        csv.Read();
        csv.ReadHeader();

        var headers = csv.HeaderRecord?.ToList() ?? new List<string>();
        return Task.FromResult(headers);
    }

    public Task<List<string[]>> ParseRowsAsync(Stream stream)
    {
        stream.Position = 0;
        using var reader = new StreamReader(stream, leaveOpen: true);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true
        });

        csv.Read();
        csv.ReadHeader();
        var columnCount = csv.HeaderRecord?.Length ?? 0;

        var rows = new List<string[]>();
        while (csv.Read())
        {
            var row = new string[columnCount];
            for (int i = 0; i < columnCount; i++)
            {
                row[i] = csv.GetField(i) ?? string.Empty;
            }
            rows.Add(row);
        }

        return Task.FromResult(rows);
    }
}
