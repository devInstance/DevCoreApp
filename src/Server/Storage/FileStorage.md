# File Storage Configuration

File storage uses a provider-based architecture. The active provider is registered in `Program.cs` via a single extension method. File metadata is tracked in the `FileRecords` database table; physical files live in the configured storage backend.

## Provider Registration

In `Program.cs`:

```csharp
builder.Services.AddLocalFileStorage(builder.Configuration);
```

To switch providers, replace this line with the desired provider's extension (e.g., `AddS3FileStorage`). Only one provider can be active at a time.

## Local File Storage (Default)

Stores files on disk in a date-partitioned directory structure:

```
storage/
  2026/
    03/
      03/
        6c9f43285cb847cb_report.csv
        a1b2c3d4e5f67890_photo.jpg
```

### appsettings.json

```json
{
  "StorageConfiguration": {
    "BasePath": "C:/app/storage",
    "BaseUrl": "https://cdn.example.com/files"
  }
}
```

| Key | Default | Description |
|-----|---------|-------------|
| `BasePath` | `{CurrentDirectory}/storage` | Root directory for file storage. Created automatically if missing. |
| `BaseUrl` | *(none)* | Optional base URL for `GetUrlAsync()`. If omitted, returns the raw storage path. |

Both keys are read from the `StorageConfiguration` section of `IConfiguration`.

**When no `StorageConfiguration` section is present** (the current default), files are stored in `{WebService project root}/storage/` and `GetUrlAsync()` returns relative paths.

## S3 File Storage (Stub)

An S3 provider skeleton exists at `src/Server/Storage/S3/` but is **not yet implemented** — all methods throw `NotImplementedException`. To complete it, implement `S3FileStorageProvider` against `IFileStorageProvider` and add an AWS S3 SDK dependency to `StorageProcessor.S3.csproj`.

Registration would be:

```csharp
builder.Services.AddS3FileStorage(builder.Configuration);
```

With configuration:

```json
{
  "StorageConfiguration": {
    "BucketName": "my-app-bucket",
    "BaseUrl": "https://my-app-bucket.s3.amazonaws.com"
  }
}
```

## Runtime Settings (via Settings Table)

These are managed through the admin Settings page at runtime and stored in the `Settings` database table. They take effect immediately without restart.

| Category | Key | Type | Default | Description |
|----------|-----|------|---------|-------------|
| `Storage` | `MaxFileSizeBytes` | `long` | `10485760` (10 MB) | Maximum upload size in bytes. Files exceeding this are rejected. |
| `Storage` | `AllowedContentTypes` | `string` | `*` | Comma-separated MIME types (e.g., `text/csv,application/pdf`). `*` allows all types. |
| `Storage` | `SoftDelete` | `bool` | `false` | When `true`, `DeleteAsync` marks the record inactive instead of removing the file from disk. |

## Adding a New Storage Provider

1. Create a new project under `src/Server/Storage/` (follow the Local/S3 structure).

2. Implement `IFileStorageProvider`:

```csharp
public interface IFileStorageProvider
{
    Task<FileUploadResult> UploadAsync(Stream stream, string fileName, string contentType, CancellationToken ct = default);
    Task<Stream> DownloadAsync(string storagePath, CancellationToken ct = default);
    Task DeleteAsync(string storagePath, CancellationToken ct = default);
    Task<string> GetUrlAsync(string storagePath, TimeSpan? expiry = null, CancellationToken ct = default);
    Task<bool> ExistsAsync(string storagePath, CancellationToken ct = default);
}
```

`UploadAsync` must return a `FileUploadResult` with `Success`, `StoragePath` (provider-specific path string), and `SizeBytes`.

3. Create a `ConfigurationExtensions.cs` with an `Add{Provider}FileStorage` method that reads from `StorageConfiguration` and registers the provider as `IFileStorageProvider`.

4. Reference the new project from `DevCoreApp.Admin.WebService.csproj` and swap the registration in `Program.cs`.

## Service Layer

`IFileService` (`src/Server/Admin/Services/Files/`) wraps the provider with business logic:

- **Validation** — checks content type and file size against runtime Settings before upload
- **Organization scoping** — sets `OrganizationId` on every `FileRecord` from `IOperationContext`
- **Entity linking** — optional `entityType`/`entityId` parameters associate files with domain records
- **Soft delete** — controlled by the `Storage:SoftDelete` setting

## API Endpoints

All endpoints require authentication (`[Authorize]`).

```
POST   /api/files/upload                Upload a file (multipart form: file, entityType?, entityId?)
                                        Returns: FileRecordItem

GET    /api/files/{publicId}/download   Download a file
                                        Returns: file stream

DELETE /api/files/{publicId}            Delete a file (soft or hard per settings)
                                        Returns: bool

GET    /api/files/{publicId}/url        Get a shareable URL
                                        Query: expiryMinutes?
                                        Returns: URL string
```

## Project Structure

```
src/Server/Storage/
  Processor/                          # Abstractions (IFileStorageProvider, FileUploadResult, StorageConfiguration)
    StorageProcessor.csproj
  Local/                              # Local disk provider
    LocalFileStorageProvider.cs
    ConfigurationExtensions.cs
    StorageProcessor.Local.csproj
  S3/                                 # AWS S3 provider (stub)
    S3FileStorageProvider.cs
    ConfigurationExtensions.cs
    StorageProcessor.S3.csproj

src/Server/Admin/Services/Files/      # Business logic
  IFileService.cs
  FileService.cs
  FileDownloadResult.cs

src/Server/Database/Core/
  Models/Files/FileRecord.cs          # Entity (DatabaseEntityObject + IOrganizationScoped)
  Data/Queries/IFileRecordQuery.cs    # Query interface
  Data/Queries/BasicsImplementation/CoreFileRecordQuery.cs
  Data/Decorators/FileRecordDecorators.cs

src/Shared/Model/Files/
  FileRecordItem.cs                   # ViewModel

src/Server/Admin/WebService/
  Controllers/FileController.cs       # API endpoints
```
