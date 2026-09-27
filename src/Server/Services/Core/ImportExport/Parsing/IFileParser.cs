namespace DevInstance.DevCoreApp.Server.Services.Core.ImportExport.Parsing;

public interface IFileParser
{
    Task<List<string>> ParseHeadersAsync(Stream stream);
    Task<List<string[]>> ParseRowsAsync(Stream stream);
}
