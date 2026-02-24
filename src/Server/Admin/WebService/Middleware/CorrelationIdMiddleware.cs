using Serilog.Context;

namespace DevInstance.DevCoreApp.Server.Admin.WebService.Middleware;

/// <summary>
/// Generates a unique correlation ID for each HTTP request and enriches
/// all Serilog log entries with it via LogContext.PushProperty.
///
/// The ID is sourced from the incoming X-Correlation-ID header (if present),
/// otherwise a new GUID is generated. The resolved ID is:
///   1. Stored in HttpContext.Items["CorrelationId"] for downstream services
///   2. Added to the response as X-Correlation-ID header
///   3. Pushed into the Serilog LogContext so every log entry includes it
/// </summary>
public class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-ID";
    public const string ItemKey = "CorrelationId";
    public const string LogPropertyName = "CorrelationId";

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[HeaderName].FirstOrDefault()
            ?? Guid.NewGuid().ToString("D");

        context.Items[ItemKey] = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        using (LogContext.PushProperty(LogPropertyName, correlationId))
        {
            await _next(context);
        }
    }
}

public static class CorrelationIdMiddlewareExtensions
{
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<CorrelationIdMiddleware>();
    }
}
