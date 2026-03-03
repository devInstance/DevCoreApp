using System.Text.Json;
using DevInstance.DevCoreApp.Server.Admin.Services.Background.Requests;
using DevInstance.DevCoreApp.Server.Admin.Services.ImportExport;
using DevInstance.DevCoreApp.Server.Database.Core.Data;
using DevInstance.DevCoreApp.Server.Database.Core.Data.Decorators;
using DevInstance.DevCoreApp.Shared.Model.ImportExport;
using DevInstance.LogScope;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DevInstance.DevCoreApp.Server.Admin.Services.Background.Tasks.Handlers;

public class ImportDataTaskHandler : IBackgroundTaskHandler
{
    public string TaskType => BackgroundTaskTypes.ImportData;

    private readonly IScopeLog log;

    public ImportDataTaskHandler(IScopeManager logManager)
    {
        log = logManager.CreateLogger(this);
    }

    public async Task HandleAsync(string payload, IServiceProvider scopedProvider, CancellationToken cancellationToken)
    {
        using var l = log.TraceScope();

        var request = JsonSerializer.Deserialize<ImportDataRequest>(payload)
            ?? throw new InvalidOperationException("Failed to deserialize import data request payload.");

        if (string.IsNullOrEmpty(request.SessionId))
            throw new InvalidOperationException("SessionId is required.");

        var repository = scopedProvider.GetRequiredService<IQueryRepository>();
        var sessionQuery = repository.GetImportSessionQuery(null!);
        var session = await sessionQuery.ByPublicId(request.SessionId).Select().FirstOrDefaultAsync(cancellationToken);

        if (session == null)
        {
            l.E($"Import session {request.SessionId} not found.");
            return;
        }

        try
        {
            var importExportService = scopedProvider.GetRequiredService<IImportExportService>();

            if (importExportService is ImportExportService concreteService)
            {
                await concreteService.CommitInternalAsync(session);
            }
            else
            {
                l.E($"Could not resolve ImportExportService for background commit.");
                session.Status = ImportSessionStatus.Failed;
                session.ErrorMessage = "Background import processing is not available in mock mode.";
                var updateQuery = repository.GetImportSessionQuery(null!);
                await updateQuery.UpdateAsync(session);
            }

            l.I($"Import session {request.SessionId} background processing completed.");
        }
        catch (Exception ex)
        {
            l.E($"Import session {request.SessionId} background processing failed: {ex.Message}");

            session.Status = ImportSessionStatus.Failed;
            session.ErrorMessage = ex.Message;
            var updateQuery = repository.GetImportSessionQuery(null!);
            await updateQuery.UpdateAsync(session);

            throw;
        }
    }
}
