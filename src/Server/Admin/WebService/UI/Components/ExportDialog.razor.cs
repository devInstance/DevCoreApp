using DevInstance.DevCoreApp.Server.Admin.Services.ImportExport;
using DevInstance.DevCoreApp.Shared.Model.ImportExport;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace DevInstance.DevCoreApp.Server.Admin.WebService.UI.Components;

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
    private bool IsExporting { get; set; }

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

    private async Task OnExport()
    {
        IsExporting = true;

        try
        {
            var request = new ExportRequestItem
            {
                EntityType = EntityType,
                Format = SelectedFormat,
                SelectedFields = SelectedFields.ToList(),
                Search = Search,
                SortBy = SortBy
            };

            var result = await ImportExportService.ExportAsync(request);
            var download = result.Result;

            // Convert stream to byte array for JS download
            using var memStream = new MemoryStream();
            await download.Stream.CopyToAsync(memStream);
            var bytes = memStream.ToArray();

            await JS.InvokeVoidAsync("downloadFileFromBytes", download.FileName, download.ContentType, bytes);

            await Close();
        }
        finally
        {
            IsExporting = false;
        }
    }

    private async Task Close()
    {
        Fields = null;
        SelectedFields.Clear();
        await OnClose.InvokeAsync();
    }
}
