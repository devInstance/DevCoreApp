using DevInstance.DevCoreApp.Client.Services.Api;
using DevInstance.LogScope;

namespace DevInstance.DevCoreApp.Client.Services;

public class ToolbarService : IToolbarService
{
    public event ToolbarEventHandler ShrinkSidebar;
    public event ToolbarEventHandler ToolbarHasChanged;

    public string Title { get; private set; }
    public bool IsSidebarShrank { get; set; }

    private IScopeLog log;

    public ToolbarService(IScopeManager lp)
    {
        log = lp.CreateLogger(this);
    }

    public void Update()
    {
        using (var l = log.TraceScope())
        {
            ToolbarHasChanged?.Invoke(null);
        }
    }

    public void ToggleSidebar()
    {
        using (var l = log.TraceScope())
        {
            IsSidebarShrank = !IsSidebarShrank;
            ShrinkSidebar?.Invoke(IsSidebarShrank);
        }
    }

    public void SetSidebar(bool isSidebarShrank)
    {
        using (var l = log.TraceScope())
        {
            IsSidebarShrank = isSidebarShrank;
            ShrinkSidebar?.Invoke(IsSidebarShrank);
        }
    }
}
