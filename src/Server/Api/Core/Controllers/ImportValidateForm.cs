using System.ComponentModel.DataAnnotations;
using DevInstance.DevCoreApp.Shared.Model.Core.ImportExport;

namespace DevInstance.DevCoreApp.Server.Api.Core.Controllers;

/// <summary>
/// Multipart body of <c>POST api/import-export/import/validate</c>, bound by MVC from the form:
/// <c>File</c>, <c>EntityType</c>, optional <c>OrganizationId</c>, and the column mappings as
/// indexed fields (<c>Mappings[0].SourceColumnIndex</c>, <c>Mappings[0].SourceColumnName</c>,
/// <c>Mappings[0].TargetField</c>, …).
/// </summary>
/// <remarks>
/// Lives in the Api project, not Shared.Model, because <see cref="IFormFile"/> is an HTTP type.
/// </remarks>
public class ImportValidateForm
{
    [Required]
    public IFormFile File { get; set; } = default!;

    [Required]
    public string EntityType { get; set; } = string.Empty;

    /// <summary>Target organization, for callers without a primary one.</summary>
    public string? OrganizationId { get; set; }

    public List<ImportColumnMappingItem> Mappings { get; set; } = new();
}
