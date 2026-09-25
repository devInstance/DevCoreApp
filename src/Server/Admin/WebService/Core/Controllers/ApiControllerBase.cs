using DevInstance.BlazorToolkit.Services;
using DevInstance.WebServiceToolkit.Controllers;
using DevInstance.WebServiceToolkit.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace DevInstance.DevCoreApp.Server.Admin.WebService.Core.Controllers;

/// <summary>
/// Base class for every <c>/api</c> controller. Controllers stay thin: each action makes exactly
/// one service call through <see cref="HandleServiceAsync{T}"/> and does no mapping or checks of
/// its own.
///
/// Wire contract (see docs/WasmMigrationPlan.md, D2):
/// <list type="bullet">
/// <item>Success → 200 with the bare <c>ServiceActionResult.Result</c> (no envelope) — what
/// BlazorToolkit's <c>IApiContext&lt;T&gt;</c> deserializes.</item>
/// <item>Failure → status code + WebServiceToolkit <see cref="WebServiceError"/> body, which
/// BlazorToolkit reads as <c>ServiceActionError</c>. Thrown exceptions are mapped by
/// <c>HandleWebRequestAsync</c>; a returned failed <see cref="ServiceActionResult{T}"/> is turned
/// into the equivalent exception by <see cref="Unwrap{T}"/>.</item>
/// </list>
/// </summary>
[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>
    /// Runs one service call and converts its result to the API wire contract.
    /// </summary>
    protected Task<ActionResult<T>> HandleServiceAsync<T>(Func<Task<ServiceActionResult<T>>> serviceCall)
    {
        return this.HandleWebRequestAsync<T>(async () => Ok(Unwrap(await serviceCall())));
    }

    /// <summary>
    /// Synchronous counterpart of <see cref="HandleServiceAsync{T}"/> for service methods that
    /// do not return a task.
    /// </summary>
    protected ActionResult<T> HandleService<T>(Func<ServiceActionResult<T>> serviceCall)
    {
        return this.HandleWebRequest<T>(() => Ok(Unwrap(serviceCall())));
    }

    /// <summary>
    /// Returns the result of a successful call, or throws the WebServiceToolkit exception that
    /// maps a failed one to its HTTP status:
    /// not authorized → 403 (the caller already passed authentication to reach the action);
    /// validation error → 400 naming the property; general error → 422;
    /// unknown/exception → 500 with the message hidden outside Development.
    /// </summary>
    protected static T Unwrap<T>(ServiceActionResult<T> result)
    {
        if (result.Success)
        {
            return result.Result!;
        }

        if (!result.IsAuthorized)
        {
            throw new ForbiddenException();
        }

        var error = result.Errors?.FirstOrDefault();
        var message = error?.Message ?? "The operation failed.";

        throw error?.ErrorType switch
        {
            ServiceActionErrorType.Validation when !string.IsNullOrEmpty(error.PropertyName)
                => new BadRequestException(message, error.PropertyName),
            ServiceActionErrorType.Validation => new BadRequestException(message),
            ServiceActionErrorType.General => new UnprocessableEntityException(message),
            _ => new InvalidOperationException(message)
        };
    }
}
