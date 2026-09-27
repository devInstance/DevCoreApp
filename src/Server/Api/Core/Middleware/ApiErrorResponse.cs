using DevInstance.WebServiceToolkit.Controllers;

namespace DevInstance.DevCoreApp.Server.Api.Core.Middleware;

/// <summary>
/// Error body for exceptions that escape a controller action: the WebServiceToolkit
/// <see cref="WebServiceError"/> shape (which BlazorToolkit clients read as <c>ServiceActionError</c>)
/// plus the request's correlation id, so a client-reported error can be joined to the server log.
/// </summary>
public class ApiErrorResponse : WebServiceError
{
    public string? CorrelationId { get; set; }
}
