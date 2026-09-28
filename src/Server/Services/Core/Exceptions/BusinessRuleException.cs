using DevInstance.WebServiceToolkit.Exceptions;

namespace DevInstance.DevCoreApp.Server.Services.Core.Exceptions;

/// <summary>
/// Thrown when a domain/business rule validation fails. Maps to HTTP 422 Unprocessable Entity
/// through WebServiceToolkit's <see cref="UnprocessableEntityException"/> — both in
/// <c>HandleWebRequestAsync</c> and in <c>ApiExceptionHandler</c>.
/// </summary>
public class BusinessRuleException : UnprocessableEntityException
{
    public BusinessRuleException(string message, string? propertyName = null) : base(message, propertyName) { }
}
