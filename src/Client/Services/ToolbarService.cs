﻿using DevInstance.LogScope;

namespace DevInstance.SampleWebApp.Client.Services
{
    public delegate void ToolbarEventHandler(object value);

    public class ToolbarService
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
}