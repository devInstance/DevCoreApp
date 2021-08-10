using DevInstance.LogScope;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DevInstance.SampleWebApp.Client.UI.Components
{
    public partial class PageTitle
    {
        private IScopeLog log;
        //TODO: figure out how to inject script of 'blazor_setTitle' from index.html as part of this component
        // so everything is in ne place
        public const string TitleFunc = "blazor_setTitle";

        protected override void OnInitialized()
        {
            log = ScopeManager.CreateLogger(this);
            using (var l = log.TraceScope())
            {
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            using (var l = log.TraceScope())
            {
                l.T($"Title: {Value}");
                if (String.IsNullOrEmpty(Value))
                {
                    await JSRuntime.InvokeVoidAsync(TitleFunc, AppName);
                }
                else
                {
                    await JSRuntime.InvokeVoidAsync(TitleFunc, String.Format(Format, AppName, Value));
                }
            }
        }
    }
}
