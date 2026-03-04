using DevInstance.DevCoreApp.Shared.Model.ImportExport;

namespace DevInstance.DevCoreApp.Server.Admin.Services.ImportExport;

public interface IImportHandler
{
    string EntityType { get; }
    string? UniqueKeyField { get; }
    List<ImportFieldDescriptor> GetFieldDescriptors();
    Task<ImportRowValidation> ValidateRowAsync(Dictionary<string, string?> mappedValues, IServiceProvider scopedProvider);
    Task<ImportCommitResult> CommitAsync(List<Dictionary<string, string?>> validRows, IServiceProvider scopedProvider);
    Task RollbackAsync(List<string> recordIds, IServiceProvider scopedProvider);
}

public interface IImportHandler<T> : IImportHandler where T : class { }
