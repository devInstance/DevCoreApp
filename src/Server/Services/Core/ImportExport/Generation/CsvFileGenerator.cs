using CsvHelper;

namespace DevInstance.DevCoreApp.Server.Services.Core.ImportExport.Generation;

public class CsvFileGenerator : IFileGenerator
{
    private static readonly CsvWriterOptions Options = new()
    {
        // Padded values stay quoted, so they read back unchanged in other tools.
        Quoting = CsvQuoting.WhenNeededOrWhitespace
    };

    public Task<Stream> GenerateAsync(List<string> headers, List<Dictionary<string, string?>> rows)
    {
        var stream = new MemoryStream();

        // CsvWriter refuses to end a record with no fields.
        if (headers.Count == 0)
        {
            return Task.FromResult<Stream>(stream);
        }

        using (var csv = CsvWriter.Create(stream, Options, leaveOpen: true))
        {
            foreach (var header in headers)
            {
                csv.WriteField(header);
            }
            csv.EndRecord();

            foreach (var row in rows)
            {
                foreach (var header in headers)
                {
                    row.TryGetValue(header, out var value);
                    csv.WriteField(value ?? string.Empty);
                }
                csv.EndRecord();
            }
        }

        stream.Position = 0;
        return Task.FromResult<Stream>(stream);
    }
}
