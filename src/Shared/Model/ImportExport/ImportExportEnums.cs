namespace DevInstance.DevCoreApp.Shared.Model.ImportExport;

public enum ImportFileFormat
{
    Csv,
    Xlsx
}

public enum ExportFileFormat
{
    Csv,
    Xlsx
}

public enum ImportSessionStatus
{
    Uploaded,
    Mapped,
    Validated,
    Processing,
    Completed,
    CompletedWithErrors,
    Failed,
    Cancelled
}

public enum ImportRowStatus
{
    Valid,
    Error,
    Skipped,
    Imported
}
