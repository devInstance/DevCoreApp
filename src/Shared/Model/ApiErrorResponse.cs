namespace DevInstance.DevCoreApp.Shared.Model;

/// <summary>
/// Standardized error response returned by API endpoints.
/// </summary>
public class ApiErrorResponse
{
    public int Status { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? CorrelationId { get; set; }
}
