using System.Collections.Generic;

namespace DevInstance.DevCoreApp.Shared.Model.ImportExport;

public class ImportRowValidation
{
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public ImportRowAction Action { get; set; } = ImportRowAction.Create;
}
