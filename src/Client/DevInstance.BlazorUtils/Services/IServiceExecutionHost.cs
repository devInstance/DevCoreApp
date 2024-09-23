namespace DevInstance.BlazorUtils.Services;

public interface IServiceExecutionHost
{
    string ErrorMessage { get; set; }
    bool IsError { get; set; }
    bool InProgress { get; set; }

    void StateHasChanged();
}
