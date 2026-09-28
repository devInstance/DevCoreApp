using DevInstance.LogScope;
using DevInstance.WebServiceToolkit.Controllers;
using Microsoft.AspNetCore.Diagnostics;

namespace DevInstance.DevCoreApp.Server.Api.Core.Middleware;

/// <summary>
/// Global exception handler for API requests (/api/*).
/// Maps exceptions with WebServiceToolkit's <see cref="ControllerUtils.ToWebServiceError"/>
/// (the same mapping HandleWebRequestAsync uses) and returns a sanitized JSON
/// <see cref="ApiErrorResponse"/> with the correlation ID.
/// Non-API requests fall through to the default error page handler.
/// </summary>
public class ApiExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // Only handle API requests — let non-API requests fall through to the error page.
        // Use the ORIGINAL path: with an ExceptionHandlingPath configured, Request.Path has
        // already been rewritten to "/Error" by the time IExceptionHandlers run.
        var originalPath = httpContext.Features.Get<IExceptionHandlerPathFeature>()?.Path
            ?? httpContext.Request.Path.Value;
        if (!new PathString(originalPath).StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // Same status/body mapping as HandleWebRequestAsync, so a client sees one error shape
        // whether the exception was thrown inside or outside a controller action.
        var isDevelopment = httpContext.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment();
        var (statusCode, error) = ControllerUtils.ToWebServiceError(exception, isDevelopment);

        var correlationId = httpContext.Items[CorrelationIdMiddleware.ItemKey] as string;

        // Resolve logger from the request scope to avoid DI lifetime issues
        var log = httpContext.RequestServices.GetRequiredService<IScopeManager>().CreateLogger(this);

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            log.E($"Unhandled exception [CorrelationId={correlationId}]: {exception}");
        }
        else
        {
            log.W($"{exception.GetType().Name} [CorrelationId={correlationId}]: {exception.Message}");
        }

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/json";

        var errorResponse = new ApiErrorResponse
        {
            ErrorType = error.ErrorType,
            Message = error.Message,
            PropertyName = error.PropertyName,
            CorrelationId = correlationId
        };

        await httpContext.Response.WriteAsJsonAsync(errorResponse, cancellationToken);
        return true;
    }
}
