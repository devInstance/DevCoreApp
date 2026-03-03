using DevInstance.BlazorToolkit.Services;
using DevInstance.DevCoreApp.Server.Admin.Services.ImportExport;
using DevInstance.DevCoreApp.Shared.Model.ImportExport;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace DevInstance.DevCoreApp.Server.Admin.WebService.UI.Pages.Admin;

public enum ImportWizardStep
{
    Upload,
    MapColumns,
    Preview,
    Result
}

public partial class ImportPage
{
    [Parameter]
    public string? EntityType { get; set; }

    [Inject]
    private IImportExportService ImportExportService { get; set; } = default!;

    [CascadingParameter]
    private IServiceExecutionHost Host { get; set; } = default!;

    private ImportWizardStep CurrentStep { get; set; } = ImportWizardStep.Upload;

    private List<string>? EntityTypes { get; set; }
    private string SelectedEntityType { get; set; } = string.Empty;
    private List<ImportFieldDescriptor>? ImportFields { get; set; }
    private List<ImportColumnMappingItem>? ColumnMappings { get; set; }
    private List<string>? ParsedHeaders { get; set; }
    private ImportSessionItem? CurrentSession { get; set; }
    private ImportValidationResult? ValidationResult { get; set; }
    private ImportCommitResult? CommitResult { get; set; }
    private bool IsBackgroundProcessing { get; set; }
    private System.Timers.Timer? pollingTimer;

    protected override async Task OnInitializedAsync()
    {
        await Host.ServiceReadAsync(
            () => Task.FromResult(ImportExportService.GetImportableEntityTypes()),
            result => EntityTypes = result
        );

        if (!string.IsNullOrEmpty(EntityType))
        {
            SelectedEntityType = EntityType;
        }
    }

    private async Task OnFileSelected(InputFileChangeEventArgs e)
    {
        if (string.IsNullOrEmpty(SelectedEntityType))
            return;

        var file = e.File;
        using var stream = file.OpenReadStream(maxAllowedSize: 50 * 1024 * 1024); // 50MB max

        var memStream = new MemoryStream();
        await stream.CopyToAsync(memStream);
        memStream.Position = 0;

        await Host.ServiceReadAsync(
            async () => await ImportExportService.ParseFileAsync(memStream, file.Name, SelectedEntityType),
            result =>
            {
                ParsedHeaders = result.Headers;
                CurrentSession = result.Session;
                LoadImportFields();
                AutoMapColumns();
                CurrentStep = ImportWizardStep.MapColumns;
            }
        );
    }

    private void LoadImportFields()
    {
        var fieldsResult = ImportExportService.GetImportFields(SelectedEntityType);
        ImportFields = fieldsResult.Result;
    }

    private void AutoMapColumns()
    {
        if (ParsedHeaders == null || ImportFields == null) return;

        ColumnMappings = new List<ImportColumnMappingItem>();

        for (int i = 0; i < ParsedHeaders.Count; i++)
        {
            var header = ParsedHeaders[i];
            var matchedField = ImportFields.FirstOrDefault(f =>
                string.Equals(f.Field, header, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(f.Label, header, StringComparison.OrdinalIgnoreCase));

            ColumnMappings.Add(new ImportColumnMappingItem
            {
                SourceColumnIndex = i,
                SourceColumnName = header,
                TargetField = matchedField?.Field
            });
        }
    }

    private async Task OnValidate()
    {
        if (CurrentSession == null || ColumnMappings == null) return;

        await Host.ServiceReadAsync(
            async () => await ImportExportService.ValidateAsync(CurrentSession.Id, ColumnMappings),
            result =>
            {
                ValidationResult = result;
                CurrentStep = ImportWizardStep.Preview;
            }
        );
    }

    private async Task OnCommit()
    {
        if (CurrentSession == null) return;

        await Host.ServiceReadAsync(
            async () => await ImportExportService.CommitAsync(CurrentSession.Id),
            result =>
            {
                CommitResult = result;
                CurrentStep = ImportWizardStep.Result;

                // Check if background processing
                if (result.Errors.Count == 1 && result.Errors[0].Contains("background"))
                {
                    IsBackgroundProcessing = true;
                    StartPolling();
                }
            }
        );
    }

    private void StartPolling()
    {
        pollingTimer = new System.Timers.Timer(3000);
        pollingTimer.Elapsed += async (s, e) => await PollSessionStatus();
        pollingTimer.Start();
    }

    private async Task PollSessionStatus()
    {
        if (CurrentSession == null) return;

        try
        {
            var result = await ImportExportService.GetSessionAsync(CurrentSession.Id);
            CurrentSession = result.Result;

            if (CurrentSession?.Status == ImportSessionStatus.Completed ||
                CurrentSession?.Status == ImportSessionStatus.CompletedWithErrors ||
                CurrentSession?.Status == ImportSessionStatus.Failed)
            {
                IsBackgroundProcessing = false;
                pollingTimer?.Stop();
                pollingTimer?.Dispose();
                pollingTimer = null;

                CommitResult = new ImportCommitResult
                {
                    SessionId = CurrentSession.Id,
                    ImportedRows = CurrentSession.ImportedRows,
                    SkippedRows = CurrentSession.ErrorRows,
                    ErrorRows = 0,
                    Errors = !string.IsNullOrEmpty(CurrentSession.ErrorMessage)
                        ? new List<string> { CurrentSession.ErrorMessage }
                        : new List<string>()
                };
            }

            await InvokeAsync(StateHasChanged);
        }
        catch
        {
            // Ignore polling errors
        }
    }

    private void OnBackToUpload()
    {
        CurrentStep = ImportWizardStep.Upload;
    }

    private void OnBackToMapping()
    {
        CurrentStep = ImportWizardStep.MapColumns;
    }

    private void OnNewImport()
    {
        CurrentStep = ImportWizardStep.Upload;
        CurrentSession = null;
        ColumnMappings = null;
        ParsedHeaders = null;
        ValidationResult = null;
        CommitResult = null;
        IsBackgroundProcessing = false;
        pollingTimer?.Stop();
        pollingTimer?.Dispose();
        pollingTimer = null;
    }

    private static string GetStepLabel(ImportWizardStep step) => step switch
    {
        ImportWizardStep.Upload => "Upload",
        ImportWizardStep.MapColumns => "Map Columns",
        ImportWizardStep.Preview => "Preview",
        ImportWizardStep.Result => "Result",
        _ => step.ToString()
    };

    public void Dispose()
    {
        pollingTimer?.Stop();
        pollingTimer?.Dispose();
    }
}
