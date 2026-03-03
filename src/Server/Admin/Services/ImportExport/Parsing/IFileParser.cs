namespace DevInstance.DevCoreApp.Server.Admin.Services.ImportExport.Parsing;

public interface IFileParser
{
    Task<List<string>> ParseHeadersAsync(Stream stream);
    Task<List<string[]>> ParseRowsAsync(Stream stream);
}
