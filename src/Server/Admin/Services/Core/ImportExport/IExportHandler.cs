using DevInstance.DevCoreApp.Shared.Model.Core.ImportExport;

namespace DevInstance.DevCoreApp.Server.Admin.Services.Core.ImportExport;

public interface IExportHandler
{
    string EntityType { get; }
    List<ExportFieldDescriptor> GetFieldDescriptors();
    Task<List<Dictionary<string, string?>>> GetExportDataAsync(
        List<string> selectedFields, string? search, string[]? sortBy, IServiceProvider scopedProvider);
}

public interface IExportHandler<T> : IExportHandler where T : class { }
