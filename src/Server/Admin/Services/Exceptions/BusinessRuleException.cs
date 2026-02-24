namespace DevInstance.DevCoreApp.Server.Admin.Services.Exceptions;

/// <summary>
/// Thrown when a domain/business rule validation fails. Maps to HTTP 422 Unprocessable Entity.
/// </summary>
public class BusinessRuleException : Exception
{
    public BusinessRuleException(string message) : base(message) { }
    public BusinessRuleException(string message, Exception innerException) : base(message, innerException) { }
}
