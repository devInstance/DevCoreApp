using DevInstance.DevCoreApp.Server.Admin.Services.ImportExport;
using DevInstance.DevCoreApp.Shared.Model.ImportExport;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace DevInstance.DevCoreApp.Server.Admin.WebService.UI.Components;

public enum ExportPhase
{
    Configure,
    Exporting,
    Complete,
    Error
}

public partial class ExportDialog
{
    [Parameter]
    public string EntityType { get; set; } = string.Empty;

    [Parameter]
    public string? Search { get; set; }

    [Parameter]
    public string[]? SortBy { get; set; }

    [Parameter]
    public bool IsVisible { get; set; }

    [Parameter]
    public EventCallback OnClose { get; set; }

    [Inject]
    private IImportExportService ImportExportService { get; set; } = default!;

    [Inject]
    private IJSRuntime JS { get; set; } = default!;

    private List<ExportFieldDescriptor>? Fields { get; set; }
    private HashSet<string> SelectedFields { get; set; } = new();
    private ExportFileFormat SelectedFormat { get; set; } = ExportFileFormat.Csv;

    private ExportPhase ExportState { get; set; } = ExportPhase.Configure;
    private string StatusMessage { get; set; } = string.Empty;
    private string ProgressDetail { get; set; } = string.Empty;
    private int ProgressPercent { get; set; }
    private string? ExportedFileName { get; set; }
    private int ExportedRowCount { get; set; }
    private string? ErrorMessage { get; set; }

    protected override void OnParametersSet()
    {
        if (IsVisible && Fields == null)
        {
            LoadFields();
        }
    }

    private void LoadFields()
    {
        var result = ImportExportService.GetExportFields(EntityType);
        Fields = result.Result;

        SelectedFields = Fields?
            .Where(f => f.IsDefault)
            .Select(f => f.Field)
            .ToHashSet() ?? new();
    }

    private void ToggleField(string field, bool isChecked)
    {
        if (isChecked)
            SelectedFields.Add(field);
        else
            SelectedFields.Remove(field);
    }

    private void SelectAllFields()
    {
        if (Fields == null) return;
        SelectedFields = Fields.Select(f => f.Field).ToHashSet();
    }

    private void DeselectAllFields()
    {
        SelectedFields.Clear();
    }

    private async Task OnExport()
    {
        ExportState = ExportPhase.Exporting;
        ErrorMessage = null;

        try
        {
            // Phase 1: Preparing
            StatusMessage = "Preparing export...";
            ProgressDetail = $"{SelectedFields.Count} fields, {SelectedFormat.ToString().ToUpperInvariant()} format";
            ProgressPercent = 15;
            StateHasChanged();

            var request = new ExportRequestItem
            {
                EntityType = EntityType,
                Format = SelectedFormat,
                SelectedFields = SelectedFields.ToList(),
                Search = Search,
                SortBy = SortBy
            };

            // Phase 2: Fetching data
            StatusMessage = "Fetching data...";
            ProgressPercent = 35;
            StateHasChanged();
            await Task.Yield();

            var result = await ImportExportService.ExportAsync(request);
            var download = result.Result;

            // Phase 3: Generating file
            StatusMessage = "Generating file...";
            ProgressPercent = 70;
            StateHasChanged();
            await Task.Yield();

            using var memStream = new MemoryStream();
            await download.Stream.CopyToAsync(memStream);
            var bytes = memStream.ToArray();

            ExportedFileName = download.FileName;
            ExportedRowCount = EstimateRowCount(bytes.Length, SelectedFields.Count);

            // Phase 4: Downloading
            StatusMessage = "Downloading...";
            ProgressPercent = 90;
            StateHasChanged();
            await Task.Yield();

            await JS.InvokeVoidAsync("downloadFileFromBytes", download.FileName, download.ContentType, bytes);

            // Done
            ProgressPercent = 100;
            ExportState = ExportPhase.Complete;
        }
        catch (Exception ex)
        {
            ExportState = ExportPhase.Error;
            ErrorMessage = ex.Message;
        }
    }

    private void ResetToConfig()
    {
        ExportState = ExportPhase.Configure;
        StatusMessage = string.Empty;
        ProgressDetail = string.Empty;
        ProgressPercent = 0;
        ExportedFileName = null;
        ExportedRowCount = 0;
        ErrorMessage = null;
    }

    private async Task Close()
    {
        ResetToConfig();
        Fields = null;
        SelectedFields.Clear();
        await OnClose.InvokeAsync();
    }

    private static int EstimateRowCount(int byteSize, int fieldCount)
    {
        if (fieldCount <= 0) return 0;
        var avgRowSize = fieldCount * 30;
        if (avgRowSize <= 0) return 0;
        var estimate = Math.Max(0, (byteSize / avgRowSize) - 1);
        return estimate;
    }

}
