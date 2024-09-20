using Microsoft.AspNetCore.Components;

namespace DevInstance.DevCoreApp.Client.UI.Components;

public partial class ErrorMessageBanner
{
    [Parameter]
    public bool IsError { get; set; }

    private string errorMessage;

    [Parameter]
    public string Message { get => errorMessage; set => errorMessage = value; }
}
