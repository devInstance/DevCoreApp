namespace DevInstance.DevCoreApp.Server.Services.Core.ImportExport.Generation;

public interface IFileGenerator
{
    Task<Stream> GenerateAsync(List<string> headers, List<Dictionary<string, string?>> rows);
}
