namespace DevInstance.BlazorUtils.Services;

public class ServiceActionResult<T>
{
    public T? Result { get; set; }

    public bool Success { get; set; }

    public ServiceActionError[]? Errors { get; set; }

    public bool IsAuthorized { get; set; }
}
