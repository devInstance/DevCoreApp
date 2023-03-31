namespace DevInstance.DevCoreApp.Client.Services;

public class ServiceActionResult<T>
{
    public T? Result { get; set; }

    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}

