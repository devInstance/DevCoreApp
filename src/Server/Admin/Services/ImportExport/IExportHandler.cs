using DevInstance.DevCoreApp.Shared.Model.ImportExport;

namespace DevInstance.DevCoreApp.Server.Admin.Services.ImportExport;

public interface IExportHandler
{
    string EntityType { get; }
    List<ExportFieldDescriptor> GetFieldDescriptors();
    Task<List<Dictionary<string, string?>>> GetExportDataAsync(
        List<string> selectedFields, string? search, string[]? sortBy, IServiceProvider scopedProvider);
}

public interface IExportHandler<T> : IExportHandler where T : class { }
