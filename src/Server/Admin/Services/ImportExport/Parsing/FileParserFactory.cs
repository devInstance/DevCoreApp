using DevInstance.DevCoreApp.Shared.Model.ImportExport;

namespace DevInstance.DevCoreApp.Server.Admin.Services.ImportExport.Parsing;

public static class FileParserFactory
{
    public static IFileParser Create(ImportFileFormat format)
    {
        return format switch
        {
            ImportFileFormat.Csv => new CsvFileParser(),
            ImportFileFormat.Xlsx => new ExcelFileParser(),
            _ => throw new ArgumentException($"Unsupported file format: {format}")
        };
    }

    public static ImportFileFormat DetectFormat(string fileName)
    {
        var extension = Path.GetExtension(fileName)?.ToLowerInvariant();
        return extension switch
        {
            ".csv" => ImportFileFormat.Csv,
            ".xlsx" => ImportFileFormat.Xlsx,
            _ => throw new ArgumentException($"Unsupported file extension: {extension}")
        };
    }
}
