namespace DevInstance.DevCoreApp.Server.Admin.Services.Core.ImportExport.Generation;

public interface IFileGenerator
{
    Task<Stream> GenerateAsync(List<string> headers, List<Dictionary<string, string?>> rows);
}
