using Microsoft.AspNetCore.Components;

namespace DevInstance.DevCoreApp.Client.UI.Components.Modals;

public partial class DeleteModalOverlay
{
    [Parameter]
    public string Id { get; set; } = "addDeleteModal";

    [Parameter]
    public string Name { get; set; }

    [Parameter]
    public EventCallback Delete { get; set; }

    public void OnDelete()
    {
        Delete.InvokeAsync();
    }
}
