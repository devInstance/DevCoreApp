using System.Net;
using DevInstance.DevCoreApp.Server.Admin.Services.Exceptions;
using DevInstance.DevCoreApp.Shared.Model;
using DevInstance.LogScope;
using DevInstance.WebServiceToolkit.Exceptions;
using Microsoft.AspNetCore.Diagnostics;

namespace DevInstance.DevCoreApp.Server.Admin.WebService.Middleware;

/// <summary>
/// Global exception handler for API requests (/api/*).
/// Maps known exception types to appropriate HTTP status codes and returns
/// a sanitized JSON response with the correlation ID.
/// Non-API requests fall through to the default error page handler.
/// </summary>
public class ApiExceptionHandler : IExceptionHandler
{
    private readonly IScopeLog _log;

    public ApiExceptionHandler(IScopeManager logManager)
    {
        _log = logManager.CreateLogger(this);
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // Only handle API requests — let non-API requests fall through to the error page
        if (!httpContext.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var (statusCode, message) = MapException(exception);

        var correlationId = httpContext.Items[CorrelationIdMiddleware.ItemKey] as string;

        if (statusCode == (int)HttpStatusCode.InternalServerError)
        {
            _log.E($"Unhandled exception [CorrelationId={correlationId}]: {exception}");
        }
        else
        {
            _log.W($"{exception.GetType().Name} [CorrelationId={correlationId}]: {exception.Message}");
        }

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/json";

        var errorResponse = new ApiErrorResponse
        {
            Status = statusCode,
            Message = message,
            CorrelationId = correlationId
        };

        await httpContext.Response.WriteAsJsonAsync(errorResponse, cancellationToken);
        return true;
    }

    private static (int StatusCode, string Message) MapException(Exception exception)
    {
        return exception switch
        {
            BadRequestException e => ((int)HttpStatusCode.BadRequest, e.Message),
            UnauthorizedException e => ((int)HttpStatusCode.Unauthorized, e.Message),
            RecordNotFoundException e => ((int)HttpStatusCode.NotFound, e.Message),
            RecordConflictException e => ((int)HttpStatusCode.Conflict, e.Message),
            BusinessRuleException e => (StatusCodes.Status422UnprocessableEntity, e.Message),
            _ => ((int)HttpStatusCode.InternalServerError, "An unexpected error occurred.")
        };
    }
}
