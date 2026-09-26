using System.Collections.Generic;

namespace DevInstance.DevCoreApp.Shared.Model.Core.ImportExport;

/// <summary>Body of <c>POST api/import-export/import/{sessionId}/commit</c>.</summary>
public class ImportCommitRequest
{
    /// <summary>1-based row numbers to leave out of the import.</summary>
    public List<int> ExcludedRows { get; set; }
}
