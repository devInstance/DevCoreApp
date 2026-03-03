using System.Globalization;
using CsvHelper;

namespace DevInstance.DevCoreApp.Server.Admin.Services.ImportExport.Generation;

public class CsvFileGenerator : IFileGenerator
{
    public Task<Stream> GenerateAsync(List<string> headers, List<Dictionary<string, string?>> rows)
    {
        var stream = new MemoryStream();
        using (var writer = new StreamWriter(stream, leaveOpen: true))
        using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
        {
            foreach (var header in headers)
            {
                csv.WriteField(header);
            }
            csv.NextRecord();

            foreach (var row in rows)
            {
                foreach (var header in headers)
                {
                    row.TryGetValue(header, out var value);
                    csv.WriteField(value ?? string.Empty);
                }
                csv.NextRecord();
            }
        }

        stream.Position = 0;
        return Task.FromResult<Stream>(stream);
    }
}
