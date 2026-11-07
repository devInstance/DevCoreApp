---
origin: ThreadIQ
targets: [DevCoreApp]
scope:
  - Server.Services.Core.ImportExport.Parsing.CsvFileParser
  - Server.Services.Core.ImportExport.Generation.CsvFileGenerator
  - Server.Services project file                     # CsvHelper 33.* -> 34.0.0
status: applied
change: "d912e40"   # ThreadIQ main: "#1276: Package update failed"
related:
  - CsvHelper changelog: https://joshclose.github.io/CsvHelper/changelog.html
---

# Port the CSV import/export to CsvHelper 34 without changing how files are read or written

## Delivery & audit log — DevCoreApp

| Date | Event |
|---|---|
| 2026-11-02 | Authored in ThreadIQ `out/`. Not yet delivered. `change` is still a placeholder. |
| 2026-11-07 | Applied in DevCoreApp: parser/generator ported, CsvHelper 34.0.0, `CsvFileFormatTests` added to `tests/Server/Api`. No deviations. Only the CsvHelper line was taken from d912e40 (no other package bumps). No DevCoreApp caller catches the old CsvHelper exception types. Empty-upload open question still unverified. |

## Why

CsvHelper 34 is a full API rewrite, and a routine NuGet update breaks the build:

```
CsvFileParser.cs(3,17): error CS0234: The type or namespace name 'Configuration' does not exist in the namespace 'CsvHelper'
```

Any fork with a floating or updated reference hits this as soon as it restores 34. Both affected classes
are shared `Core` code, so the fix belongs in canonical `Core`.

**Fixing only the compile errors is not enough.** v34's defaults differ from v33's in ways that change how
a customer's file is imported, and no compile error or existing test reveals it:

| Input | v33 | v34 defaults |
|---|---|---|
| Separator | Fixed by the culture (comma) | Detected from the file, among `,` `;` tab `\|` |
| `sep=;` first line | An ordinary header row | Sets the separator and is skipped |
| Stray or unclosed quote | Throws | Recovered silently |
| Row shorter than the header | Throws | Missing fields read as empty |
| Value with leading or trailing spaces, on export | Quoted | Not quoted |

Silently repaired input means an import can commit wrong data where it used to fail.

## Current state

ThreadIQ, before this change. DevCoreApp should have the same two classes; check them against this.

| Surface | Pattern |
|---|---|
| `…ImportExport.Parsing.CsvFileParser` | `new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = true })`, `ReadHeader()`, `HeaderRecord`, `GetField(i)` |
| `…ImportExport.Generation.CsvFileGenerator` | `new CsvWriter(writer, CultureInfo.InvariantCulture)`, `WriteField`, `NextRecord()` |
| Server.Services project file | `<PackageReference Include="CsvHelper" Version="33.*" />` |

These are the only two files in ThreadIQ that use CsvHelper. They had no tests.

## Proposed change

Reference `CsvHelper` `34.0.0` and replace the API use as below. The namespace `CsvHelper.Configuration`,
`CsvConfiguration`, `HeaderRecord`, `GetField` and `NextRecord` no longer exist. The `StreamReader` and
`StreamWriter` wrappers go too, because v34 reads and writes the stream directly (UTF-8, byte order mark
honoured on read, none written).

### `CsvFileParser`

```csharp
// CsvHelper 34 defaults to sniffing the separator and silently repairing bad input. An import must
// read the columns the file declares or fail, so keep the comma fixed and malformed rows fatal.
private static readonly CsvReaderOptions Options = new()
{
    HasHeader = true,
    Separator = ',',
    DetectSeparatorHint = false,
    MissingField = CsvMissingFieldHandling.Throw,
    MalformedData = CsvMalformedDataHandling.Throw
};

public Task<List<string>> ParseHeadersAsync(Stream stream)
{
    stream.Position = 0;
    using var csv = CsvReader.Create(stream, Options, leaveOpen: true);
    return Task.FromResult(csv.Headers.ToList());
}

public Task<List<string[]>> ParseRowsAsync(Stream stream)
{
    stream.Position = 0;
    using var csv = CsvReader.Create(stream, Options, leaveOpen: true);
    var columnCount = csv.Headers.Count;
    var rows = new List<string[]>();
    while (csv.Read())
    {
        var row = new string[columnCount];
        for (int i = 0; i < columnCount; i++)
        {
            row[i] = csv[i].ToString();
        }
        rows.Add(row);
    }
    return Task.FromResult(rows);
}
```

`leaveOpen: true` matters: the import service parses the same stream twice. `Read()` reads the header
itself, so the old `Read()` + `ReadHeader()` preamble goes away. Per the v34 docs, `ReadHeader()` on a data
record makes that record the new header, so keeping the preamble would lose the first data row.

### `CsvFileGenerator`

```csharp
private static readonly CsvWriterOptions Options = new()
{
    // Padded values stay quoted, so they read back unchanged in other tools.
    Quoting = CsvQuoting.WhenNeededOrWhitespace
};

public Task<Stream> GenerateAsync(List<string> headers, List<Dictionary<string, string?>> rows)
{
    var stream = new MemoryStream();

    // CsvWriter refuses to end a record with no fields.
    if (headers.Count == 0)
    {
        return Task.FromResult<Stream>(stream);
    }

    using (var csv = CsvWriter.Create(stream, Options, leaveOpen: true))
    {
        // WriteField as before; NextRecord() becomes EndRecord().
    }

    stream.Position = 0;
    return Task.FromResult<Stream>(stream);
}
```

### Behaviour that still differs from v33

Accepted in ThreadIQ, found by running the same fixtures through both versions:

| Case | v33 | After this change | Note |
|---|---|---|---|
| Empty CSV uploaded | `ReaderException` | 0 columns, 0 rows | The import page has not been tested with this |
| Export with no headers | One blank line | Empty file | Only reachable if an export handler has no default fields |
| Single-column export, empty value | Blank line, skipped on re-import | `""`, so the row survives re-import | An improvement |
| Short row | `MissingFieldException` | `IndexOutOfRangeException` | Still fails; ThreadIQ's callers do not catch either by type |
| Malformed quote | `BadDataException` | `CsvFormatException` | Same. Check DevCoreApp's callers for a catch on the old types |

Everything else in the fixtures matched: BOMs, UTF-16 with BOM, invalid UTF-8, quoted separators and line
breaks, blank lines, padded fields, duplicate headers, extra fields ignored, and export bytes for quoting,
null values and formula-like text (`=SUM(A1)` stays as written, no injection escaping, as in v33).

## Affected shared surface

| Sync key | Change |
|---|---|
| `Server.Services.Core.ImportExport.Parsing.CsvFileParser` | Ported, options pinned |
| `Server.Services.Core.ImportExport.Generation.CsvFileGenerator` | Ported, options pinned, empty-headers guard |
| Server.Services project file | `CsvHelper` `34.0.0` |
| `Server.Tests.Core.ImportExport.CsvFileFormatTests` | New, 13 tests |

Neither class carries a `project-specific` fence in ThreadIQ, and the change adds none. `IFileParser`,
`IFileGenerator`, `ImportExportService` and `ImportExport.md` do not change.

## Source change

```bash
git show <change> -- src/Server/Services/Core/ImportExport tests/Server/Api/Core/ImportExport
```

Take the CsvHelper line from `src/Server/Services/ThreadIQ.Server.Services.csproj` by hand. Do **not**
take the rest of the project-file changes in that commit: the same NuGet update also moved the
`Microsoft.*` packages from `10.0.13` to `10.0.14`, Microsoft.Graph, MailKit and SkiaSharp, which are not
part of this fix. Leave out this doc as well.

The diff is a reference, not a patch: every namespace carries a different product prefix in DevCoreApp.

## Verification

- `dotnet build`.
- `dotnet test --filter "FullyQualifiedName~CsvFileFormatTests"`: 13 tests covering quoted fields and blank
  lines, reading a stream twice, BOM, no separator detection (including `sep=`), malformed rows rejected,
  extra fields ignored, exact export output, no BOM on export, empty headers, and the export → import round trip.
- Manual, not done in ThreadIQ: import and export a CSV through the admin UI, and upload an empty CSV to see
  how the import page handles zero columns.

## Open questions

- **Empty upload.** A zero-column result used to be an error. If the import page handles it badly,
  `ImportExportService.ParseHeadersAsync` should reject it with a `BadRequestException`. ThreadIQ has not
  checked this.
