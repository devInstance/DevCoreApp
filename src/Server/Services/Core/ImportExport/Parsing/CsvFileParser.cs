using CsvHelper;

namespace DevInstance.DevCoreApp.Server.Services.Core.ImportExport.Parsing;

public class CsvFileParser : IFileParser
{
    // CsvHelper 34 defaults to sniffing the separator and silently repairing bad input. An import must
    // read the columns the file declares or fail, so keep the comma fixed and malformed rows fatal.
    private static readonly CsvReaderOptions Options = new()
    {
        HasHeader = true,
        Separator = ',',
        DetectSeparatorHint = false,
        MissingField = CsvMissingFieldHandling.Throw,
        MalformedData = CsvMalformedDataHandling.Throw
    };

    public Task<List<string>> ParseHeadersAsync(Stream stream)
    {
        stream.Position = 0;
        using var csv = CsvReader.Create(stream, Options, leaveOpen: true);

        var headers = csv.Headers.ToList();
        return Task.FromResult(headers);
    }

    public Task<List<string[]>> ParseRowsAsync(Stream stream)
    {
        stream.Position = 0;
        using var csv = CsvReader.Create(stream, Options, leaveOpen: true);

        var columnCount = csv.Headers.Count;

        var rows = new List<string[]>();
        while (csv.Read())
        {
            var row = new string[columnCount];
            for (int i = 0; i < columnCount; i++)
            {
                row[i] = csv[i].ToString();
            }
            rows.Add(row);
        }

        return Task.FromResult(rows);
    }
}
