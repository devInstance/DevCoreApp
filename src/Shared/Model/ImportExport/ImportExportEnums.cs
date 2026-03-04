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
    Cancelled,
    RolledBack
}

public enum ImportRowAction
{
    Create,
    Update
}

public enum ImportRowStatus
{
    Valid,
    Warning,
    Error,
    Skipped,
    Imported
}
