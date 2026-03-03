# Import/Export Engine

Generic infrastructure for importing and exporting entity data as CSV or Excel (.xlsx). Any entity type can participate by implementing a handler. The engine manages file parsing, column mapping, row validation, background processing for large imports, and file generation for exports.

**Libraries:** CsvHelper (CSV), ClosedXML (Excel) — both in `DevCoreApp.Admin.Services.csproj`.

## Architecture

```
UI (ImportPage, ExportDialog)
  -> IImportExportService (orchestration)
       -> IFileParser / IFileGenerator (CSV/Excel I/O)
       -> IImportHandler / IExportHandler (per-entity rules)
       -> IFileService (uploaded file storage)
       -> IBackgroundWorker (large import jobs)
       -> ImportSession (database entity, tracks state)
```

Handlers are registered via DI. The service discovers them from `IEnumerable<IImportHandler>` and `IEnumerable<IExportHandler>`, matching by `EntityType` string.

## Shared DTOs (`src/Shared/Model/ImportExport/`)

| Class | Purpose |
|-------|---------|
| `ImportExportEnums` | `ImportFileFormat`, `ExportFileFormat`, `ImportSessionStatus`, `ImportRowStatus` |
| `ImportFieldDescriptor` | Handler field metadata: `Field`, `Label`, `IsRequired`, `DataType`, `Description` |
| `ExportFieldDescriptor` | Export field metadata: `Field`, `Label`, `IsDefault` |
| `ImportColumnMappingItem` | User's column mapping: `SourceColumnIndex`, `SourceColumnName`, `TargetField` |
| `ImportRowPreviewItem` | Row validation result: `RowNumber`, `Status`, `Values` (dict), `Errors` (list) |
| `ImportValidationResult` | Validation summary: `SessionId`, `TotalRows`, `ValidRows`, `ErrorRows`, `Rows` |
| `ImportCommitResult` | Commit result: `SessionId`, `ImportedRows`, `SkippedRows`, `ErrorRows`, `Errors` |
| `ExportRequestItem` | Export request: `EntityType`, `Format`, `SelectedFields`, `Search`, `SortBy` |
| `ImportSessionItem` | Session DTO extending `ModelItem` — full session state for the UI |

**Note:** The Shared project does not have `ImplicitUsings` or `#nullable`. Always add explicit `using System;` / `using System.Collections.Generic;` and avoid nullable annotations.

## Database Entity

**`ImportSession`** (`src/Server/Database/Core/Models/ImportExport/`)

Inherits `DatabaseObject`, implements `IOrganizationScoped`. Tracks an import through all workflow stages.

| Field | Type | Notes |
|-------|------|-------|
| `OrganizationId` | `Guid` | From `IOperationContext` |
| `EntityType` | `string` | e.g. `"UserProfile"` |
| `OriginalFileName` | `string` | User's uploaded file name |
| `FileFormat` | `ImportFileFormat` | CSV or XLSX |
| `Status` | `ImportSessionStatus` | Current workflow stage |
| `FileRecordId` | `string?` | `PublicId` of uploaded `FileRecord` |
| `ColumnMappingJson` | `string?` | Serialized `List<ImportColumnMappingItem>` |
| `ValidationResultJson` | `string?` | Serialized row-level errors |
| `TotalRows`, `ValidRows`, `ErrorRows`, `ImportedRows` | `int` | Counters |
| `ErrorMessage` | `string?` | Failure details |
| `CreatedById` | `Guid?` | FK to `UserProfile` |

**Query:** `IImportSessionQuery` — supports `ByPublicId()`, `ByEntityType()`, `ByStatus()`, `Search()`, `SortBy()`.

**Decorator:** `ImportSessionDecorators` — `ToView()` deserializes `ColumnMappingJson`, `ToRecord()` serializes it back.

**Migration required** — the `ImportSessions` table must be added via EF migration.

## Handler Interfaces

### IImportHandler

```csharp
public interface IImportHandler
{
    string EntityType { get; }
    List<ImportFieldDescriptor> GetFieldDescriptors();
    Task<List<string>> ValidateRowAsync(
        Dictionary<string, string?> mappedValues, IServiceProvider scopedProvider);
    Task<ImportCommitResult> CommitAsync(
        List<Dictionary<string, string?>> validRows, IServiceProvider scopedProvider);
}

public interface IImportHandler<T> : IImportHandler where T : class { }
```

### IExportHandler

```csharp
public interface IExportHandler
{
    string EntityType { get; }
    List<ExportFieldDescriptor> GetFieldDescriptors();
    Task<List<Dictionary<string, string?>>> GetExportDataAsync(
        List<string> selectedFields, string? search, string[]? sortBy,
        IServiceProvider scopedProvider);
}

public interface IExportHandler<T> : IExportHandler where T : class { }
```

The non-generic base interface carries all logic. The generic `<T>` is a marker for type-safe DI identification. Handlers receive `IServiceProvider` to resolve scoped services (e.g., `IUserProfileService`, `UserManager`).

## Adding a New Entity Type

To make a new entity importable/exportable:

### 1. Create the import handler

```
src/Server/Admin/Services/ImportExport/Handlers/{Entity}ImportHandler.cs
```

```csharp
public class InvoiceImportHandler : IImportHandler<InvoiceItem>
{
    public string EntityType => "Invoice";

    public List<ImportFieldDescriptor> GetFieldDescriptors()
    {
        return new List<ImportFieldDescriptor>
        {
            new() { Field = "Number", Label = "Invoice #", IsRequired = true, DataType = "string" },
            new() { Field = "Amount", Label = "Amount", IsRequired = true, DataType = "decimal" },
            // ...
        };
    }

    public async Task<List<string>> ValidateRowAsync(
        Dictionary<string, string?> mappedValues, IServiceProvider scopedProvider)
    {
        var errors = new List<string>();
        // Validate fields, check business rules, detect duplicates
        return errors;
    }

    public async Task<ImportCommitResult> CommitAsync(
        List<Dictionary<string, string?>> validRows, IServiceProvider scopedProvider)
    {
        var service = scopedProvider.GetRequiredService<IInvoiceService>();
        var result = new ImportCommitResult();
        foreach (var row in validRows)
        {
            // Create entity via service, update result counters
        }
        return result;
    }
}
```

### 2. Create the export handler

```
src/Server/Admin/Services/ImportExport/Handlers/{Entity}ExportHandler.cs
```

```csharp
public class InvoiceExportHandler : IExportHandler<InvoiceItem>
{
    public string EntityType => "Invoice";

    public List<ExportFieldDescriptor> GetFieldDescriptors()
    {
        return new List<ExportFieldDescriptor>
        {
            new() { Field = "Number", Label = "Invoice #", IsDefault = true },
            new() { Field = "Amount", Label = "Amount", IsDefault = true },
            // ...
        };
    }

    public async Task<List<Dictionary<string, string?>>> GetExportDataAsync(
        List<string> selectedFields, string? search, string[]? sortBy,
        IServiceProvider scopedProvider)
    {
        var service = scopedProvider.GetRequiredService<IInvoiceService>();
        var rows = new List<Dictionary<string, string?>>();
        // Paginate through data, build dictionary per row with selected fields
        return rows;
    }
}
```

### 3. Register in `Program.cs`

Inside the `#if !SERVICEMOCKS` block:

```csharp
builder.Services.AddScoped<IImportHandler, InvoiceImportHandler>();
builder.Services.AddScoped<IExportHandler, InvoiceExportHandler>();
```

### 4. Add Export button to the entity's list page

```razor
<button class="btn btn-outline-primary" @onclick="ShowExportDialog">
    <i class="bi bi-download me-1"></i>Export
</button>

<ExportDialog EntityType="Invoice"
              Search="@SearchTerm"
              SortBy="@GetCurrentSortBy()"
              IsVisible="@IsExportDialogVisible"
              OnClose="HideExportDialog" />
```

The Import page (`/admin/import`) automatically discovers all registered entity types — no UI changes needed for import.

## Import Workflow

```
Upload file ──> Parse headers ──> Map columns ──> Validate rows ──> Commit
   │                 │                 │                │              │
ParseFileAsync   IFileParser     User adjusts    ValidateAsync   CommitAsync
                 + IFileService   mappings in     per-row via    (inline or
                                  wizard UI       handler        background)
```

### Step by step

1. **ParseFileAsync** — detects format, parses headers, uploads file via `IFileService`, creates `ImportSession` (Status = `Uploaded`)
2. **User maps columns** — UI auto-matches headers to field names, user adjusts
3. **ValidateAsync** — downloads file, parses rows, applies mappings, calls `handler.ValidateRowAsync()` per row, saves results on session (Status = `Validated`)
4. **CommitAsync** — if `ValidRows <= 500`: calls `handler.CommitAsync()` inline. If `> 500`: submits `ImportData` background job and returns immediately
5. **Background job** — `ImportDataTaskHandler` picks up the job, calls `CommitInternalAsync()`, updates session status

### Session status flow

```
Uploaded -> Mapped -> Validated -> Processing -> Completed
                                             -> CompletedWithErrors
                                             -> Failed
                                -> Cancelled
```

## Export Workflow

```
User selects fields/format ──> ExportAsync ──> Handler fetches data ──> File generated ──> Browser download
```

1. `ExportDialog` loads available fields from `handler.GetFieldDescriptors()`
2. User selects fields and format (CSV or XLSX)
3. `ExportAsync` calls `handler.GetExportDataAsync()` to fetch all data
4. Data is passed to `CsvFileGenerator` or `ExcelFileGenerator`
5. Generated file stream is sent to the browser via JS interop (`downloadFileFromBytes`)

## File Parsing & Generation

### Parsers (`Services/ImportExport/Parsing/`)

| Class | Format | Library |
|-------|--------|---------|
| `CsvFileParser` | CSV | CsvHelper |
| `ExcelFileParser` | XLSX | ClosedXML (first worksheet, row 1 = headers) |

**`FileParserFactory`** — `Create(ImportFileFormat)` returns the correct parser. `DetectFormat(fileName)` detects format from extension.

### Generators (`Services/ImportExport/Generation/`)

| Class | Format | Library |
|-------|--------|---------|
| `CsvFileGenerator` | CSV | CsvHelper |
| `ExcelFileGenerator` | XLSX | ClosedXML (bold headers, auto-width columns) |

Both implement `IFileGenerator`:
```csharp
Task<Stream> GenerateAsync(List<string> headers, List<Dictionary<string, string?>> rows);
```

## Background Job Integration

Large imports (> 500 valid rows) are queued as background jobs.

- **Request type:** `BackgroundRequestType.ImportData`
- **Task type:** `BackgroundTaskTypes.ImportData`
- **Payload:** `ImportDataRequest { SessionId }`
- **Handler:** `ImportDataTaskHandler` — resolves `ImportExportService`, calls `CommitInternalAsync()`
- **Result reference:** `"ImportSession:{sessionId}"`

The UI polls `GetSessionAsync()` every 3 seconds until the session reaches a terminal status.

## UI Components

### ImportPage (`UI/Pages/Admin/ImportPage.razor`)

Route: `/admin/import` or `/admin/import/{EntityType}`

Authorization: `Owner, Admin`

Four-step wizard controlled by `ImportWizardStep` enum:

| Step | UI | Action |
|------|----|--------|
| **Upload** | Entity type dropdown + file picker (`.csv`, `.xlsx`) | `ParseFileAsync()` |
| **MapColumns** | Source→target dropdown per column, auto-matching | `ValidateAsync()` |
| **Preview** | Validation results table with row-level errors | `CommitAsync()` |
| **Result** | Success/error summary, error list (top 10) | "New Import" to restart |

### ExportDialog (`UI/Components/ExportDialog.razor`)

Reusable Bootstrap modal. Parameters: `EntityType`, `Search`, `SortBy`, `IsVisible`, `OnClose`.

Four phases controlled by `ExportPhase` enum:

| Phase | UI |
|-------|----|
| **Configure** | Field checkboxes (Select All / Clear), format radio (CSV / Excel) |
| **Exporting** | Spinner, animated progress bar (15% → 35% → 70% → 90% → 100%), status text |
| **Complete** | Success alert with filename and row estimate |
| **Error** | Error alert with message, "Try Again" button |

## API Controller

```
POST /api/import-export/export     — returns file download (stream)
POST /api/import-export/import/upload — alternative upload endpoint (IFormFile + entityType query param)
```

Both endpoints require `Owner` or `Admin` role.

## Mock Service

`ImportExportServiceMock` (`mocks/Server/Admin/ServicesMocks/ImportExport/`) — annotated `[BlazorServiceMock]`. Generates fake data with Bogus, simulates 15-row imports with 3 validation errors, and produces CSV exports with fake user data. Uses `Task.Delay` for realistic latency.

## Existing Handlers

| Handler | EntityType | Fields |
|---------|-----------|--------|
| `UserProfileImportHandler` | `UserProfile` | Email (required, email), FirstName (required), MiddleName, LastName (required), PhoneNumber (phone), Role (required: Admin/Manager/Employee/Client) |
| `UserProfileExportHandler` | `UserProfile` | Email, FirstName, MiddleName, LastName, PhoneNumber, Roles, Status (all default) |

## File Summary

```
src/Shared/Model/ImportExport/
    ImportExportEnums.cs
    ImportFieldDescriptor.cs
    ExportFieldDescriptor.cs
    ImportColumnMappingItem.cs
    ImportRowPreviewItem.cs
    ImportValidationResult.cs
    ImportCommitResult.cs
    ExportRequestItem.cs
    ImportSessionItem.cs

src/Server/Database/Core/
    Models/ImportExport/ImportSession.cs
    Data/Queries/IImportSessionQuery.cs
    Data/Queries/BasicsImplementation/CoreImportSessionQuery.cs
    Data/Decorators/ImportSessionDecorators.cs

src/Server/Admin/Services/ImportExport/
    IImportExportService.cs
    ImportExportService.cs
    ExportDownloadResult.cs
    IImportHandler.cs
    IExportHandler.cs
    Parsing/IFileParser.cs
    Parsing/CsvFileParser.cs
    Parsing/ExcelFileParser.cs
    Parsing/FileParserFactory.cs
    Generation/IFileGenerator.cs
    Generation/CsvFileGenerator.cs
    Generation/ExcelFileGenerator.cs
    Handlers/UserProfileImportHandler.cs
    Handlers/UserProfileExportHandler.cs

src/Server/Admin/Services/Background/
    Requests/ImportDataRequest.cs
    Tasks/Handlers/ImportDataTaskHandler.cs

src/Server/Admin/WebService/
    Controllers/ImportExportController.cs
    UI/Pages/Admin/ImportPage.razor(.cs)
    UI/Components/ExportDialog.razor(.cs)

mocks/Server/Admin/ServicesMocks/ImportExport/
    ImportExportServiceMock.cs
```
