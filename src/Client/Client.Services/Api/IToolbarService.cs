using DevInstance.LogScope;

namespace DevInstance.DevCoreApp.Client.Services.Api;

public delegate void ToolbarEventHandler(object value);

public interface IToolbarService
{
    event ToolbarEventHandler ShrinkSidebar;
    event ToolbarEventHandler ToolbarHasChanged;

    string Title { get; }
    bool IsSidebarShrank { get; set; }

    void Update();

    void ToggleSidebar();

    void SetSidebar(bool isSidebarShrank);
}
