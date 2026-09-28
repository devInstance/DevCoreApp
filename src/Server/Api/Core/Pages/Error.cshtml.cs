using DevInstance.DevCoreApp.Server.Api.Core.Middleware;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DevInstance.DevCoreApp.Server.Api.Core.Pages;

/// <summary>
/// Target of UseExceptionHandler("/Error") for non-API requests (API errors get JSON from
/// ApiExceptionHandler). Shows the correlation id so a report can be matched to the log.
/// </summary>
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
[IgnoreAntiforgeryToken]
public class ErrorModel : PageModel
{
    public string? CorrelationId { get; private set; }

    public void OnGet() => CorrelationId = HttpContext.Items[CorrelationIdMiddleware.ItemKey] as string;

    // The exception handler re-executes the original method, so a failed POST lands here too.
    public void OnPost() => OnGet();
}
