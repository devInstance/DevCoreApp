using DevInstance.BlazorToolkit.Services;
using DevInstance.DevCoreApp.Server.Admin.Services.ImportExport;
using DevInstance.DevCoreApp.Server.Admin.Services.Organizations;
using DevInstance.DevCoreApp.Shared.Model.ImportExport;
using DevInstance.DevCoreApp.Shared.Model.Organizations;
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

    [Inject]
    private IOrganizationService OrganizationService { get; set; } = default!;

    [CascadingParameter]
    private IServiceExecutionHost Host { get; set; } = default!;

    private ImportWizardStep CurrentStep { get; set; } = ImportWizardStep.Upload;

    private List<string>? EntityTypes { get; set; }
    private string SelectedEntityType { get; set; } = string.Empty;
    private List<ImportFieldDescriptor>? ImportFields { get; set; }
    private List<ImportColumnMappingItem>? ColumnMappings { get; set; }
    private List<string>? ParsedHeaders { get; set; }
    private ImportParseResult? ParseResult { get; set; }
    private ImportSessionItem? CurrentSession { get; set; }
    private ImportValidationResult? ValidationResult { get; set; }
    private ImportCommitResult? CommitResult { get; set; }
    private MemoryStream? BufferedFileStream { get; set; }
    private string? BufferedFileName { get; set; }
    private bool IsBackgroundProcessing { get; set; }
    private System.Timers.Timer? pollingTimer;

    // Feature 4: Rollback state
    private bool ShowRollbackConfirm { get; set; }
    private bool IsRolledBack { get; set; }

    // Feature 6: Unmapped columns
    private List<string> UnmappedColumns { get; set; } = new();

    // Feature 2: Progress
    private int ProgressPercent { get; set; }
    private string ProgressLabel { get; set; } = string.Empty;

    // Organization resolution
    private bool RequiresOrganization { get; set; }
    private List<OrganizationItem>? AvailableOrganizations { get; set; }
    private string? SelectedOrganizationId { get; set; }

    // Row exclusion
    private HashSet<int> ExcludedRows { get; set; } = new();

    private int SelectedRowCount => (ValidationResult?.Rows?.Count(r =>
        r.Status != ImportRowStatus.Error && !ExcludedRows.Contains(r.RowNumber)) ?? 0);

    private bool CanRollback =>
        CommitResult != null &&
        (CommitResult.ImportedRows > 0 || CommitResult.UpdatedRows > 0) &&
        !IsRolledBack &&
        CurrentSession?.Status != ImportSessionStatus.RolledBack;

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

    private async Task OnFileChanged(InputFileChangeEventArgs e)
    {
        var file = e.File;
        BufferedFileStream?.Dispose();
        BufferedFileStream = new MemoryStream();
        using var stream = file.OpenReadStream(maxAllowedSize: 50 * 1024 * 1024); // 50MB max
        await stream.CopyToAsync(BufferedFileStream);
        BufferedFileStream.Position = 0;
        BufferedFileName = file.Name;
    }

    private async Task OnUpload()
    {
        if (string.IsNullOrEmpty(SelectedEntityType) || BufferedFileStream == null || BufferedFileName == null)
            return;

        BufferedFileStream.Position = 0;

        await Host.ServiceReadAsync(
            async () => await ImportExportService.ParseHeadersAsync(BufferedFileStream, BufferedFileName),
            result =>
            {
                ParseResult = result;
                ParsedHeaders = result.Headers;
                LoadImportFields();
                AutoMapColumns();
                DetectUnmappedColumns();
                RequiresOrganization = ImportExportService.RequiresOrganizationSelection();
                CurrentStep = ImportWizardStep.MapColumns;
            }
        );

        // Load organizations after the parse completes (avoid nesting ServiceReadAsync)
        if (RequiresOrganization)
        {
            await Host.ServiceReadAsync(
                async () => await OrganizationService.GetTreeAsync(),
                tree => AvailableOrganizations = tree
            );
        }
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

    private void DetectUnmappedColumns()
    {
        if (ColumnMappings == null) return;

        UnmappedColumns = ColumnMappings
            .Where(m => string.IsNullOrEmpty(m.TargetField))
            .Select(m => m.SourceColumnName)
            .ToList();
    }

    private async Task OnValidate()
    {
        if (ColumnMappings == null || BufferedFileStream == null || BufferedFileName == null) return;

        if (RequiresOrganization && string.IsNullOrEmpty(SelectedOrganizationId))
            return;

        BufferedFileStream.Position = 0;

        await Host.ServiceReadAsync(
            async () => await ImportExportService.ValidateAsync(
                BufferedFileStream, BufferedFileName, SelectedEntityType,
                ColumnMappings, SelectedOrganizationId),
            result =>
            {
                ValidationResult = result;
                CurrentSession = new ImportSessionItem
                {
                    Id = result.SessionId,
                    EntityType = SelectedEntityType,
                    OriginalFileName = BufferedFileName,
                    Status = ImportSessionStatus.Validated,
                    TotalRows = result.TotalRows,
                    ValidRows = result.ValidRows,
                    ErrorRows = result.ErrorRows
                };
                CurrentStep = ImportWizardStep.Preview;
            }
        );
    }

    private void ToggleRowExclusion(int rowNumber)
    {
        if (!ExcludedRows.Remove(rowNumber))
            ExcludedRows.Add(rowNumber);
    }

    private void ToggleAllRows()
    {
        if (ValidationResult?.Rows == null) return;

        var eligibleRows = ValidationResult.Rows
            .Where(r => r.Status != ImportRowStatus.Error)
            .Select(r => r.RowNumber)
            .ToList();

        if (ExcludedRows.Count < eligibleRows.Count)
            ExcludedRows = new HashSet<int>(eligibleRows);
        else
            ExcludedRows.Clear();
    }

    private async Task OnCommit()
    {
        if (CurrentSession == null) return;

        var excluded = ExcludedRows.Count > 0 ? ExcludedRows.ToList() : null;

        await Host.ServiceReadAsync(
            async () => await ImportExportService.CommitAsync(CurrentSession.Id, excluded),
            result =>
            {
                CommitResult = result;
                CurrentStep = ImportWizardStep.Result;

                // Check if background processing
                if (result.Errors.Count == 1 && result.Errors[0].Contains("background"))
                {
                    IsBackgroundProcessing = true;
                    ProgressPercent = 60;
                    ProgressLabel = "Importing rows...";
                    StartPolling();
                }
            }
        );
    }

    private async Task OnRollback()
    {
        if (CurrentSession == null) return;

        await Host.ServiceReadAsync(
            async () => await ImportExportService.RollbackAsync(CurrentSession.Id),
            _ =>
            {
                IsRolledBack = true;
                ShowRollbackConfirm = false;
                if (CurrentSession != null)
                {
                    CurrentSession.Status = ImportSessionStatus.RolledBack;
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
                ProgressPercent = 100;
                ProgressLabel = "Import complete";
                pollingTimer?.Stop();
                pollingTimer?.Dispose();
                pollingTimer = null;

                CommitResult = new ImportCommitResult
                {
                    SessionId = CurrentSession.Id,
                    ImportedRows = CurrentSession.ImportedRows,
                    UpdatedRows = CurrentSession.UpdatedRows,
                    SkippedRows = CurrentSession.ErrorRows,
                    ErrorRows = 0,
                    Errors = !string.IsNullOrEmpty(CurrentSession.ErrorMessage)
                        ? new List<string> { CurrentSession.ErrorMessage }
                        : new List<string>()
                };
            }
            else if (CurrentSession != null && CurrentSession.ValidRows > 0)
            {
                // Calculate progress: 60-95% range based on imported rows
                ProgressPercent = 60 + (int)((double)CurrentSession.ImportedRows / CurrentSession.ValidRows * 35);
                ProgressLabel = $"Importing rows... ({CurrentSession.ImportedRows} of {CurrentSession.ValidRows})";
            }

            await InvokeAsync(StateHasChanged);
        }
        catch
        {
            // Ignore polling errors
        }
    }

    private static string GetRowCssClass(ImportRowPreviewItem row)
    {
        if (row.Status == ImportRowStatus.Error)
            return "table-danger";
        if (row.Status == ImportRowStatus.Warning)
            return "table-warning";
        return "table-success";
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
        ParseResult = null;
        ValidationResult = null;
        CommitResult = null;
        BufferedFileStream?.Dispose();
        BufferedFileStream = null;
        BufferedFileName = null;
        IsBackgroundProcessing = false;
        ShowRollbackConfirm = false;
        IsRolledBack = false;
        UnmappedColumns = new();
        RequiresOrganization = false;
        AvailableOrganizations = null;
        SelectedOrganizationId = null;
        ExcludedRows = new();
        ProgressPercent = 0;
        ProgressLabel = string.Empty;
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
