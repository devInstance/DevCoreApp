
using DevInstance.LogScope;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Globalization;
using DevInstance.DevCoreApp.Client.Services.Api;

namespace DevInstance.DevCoreApp.Client.UI.Pages.Settings
{
    public partial class SettingsIndex
    {
        [Inject]
        public NavigationManager NavManager { get; set; }
        [Inject]
        public IJSRuntime JSRuntime { get; set; }
        [Inject]
        IScopeManager ScopeManager { get; set; }
        [Inject]
        ISettingsService Settings { get; set; }

        CultureInfo Culture
        {
            get => CultureInfo.CurrentCulture;
            set
            {
                if (CultureInfo.CurrentCulture != value)
                {
                    var js = (IJSInProcessRuntime)JSRuntime;
                    js.InvokeVoid("blazor_setCulture", value.Name);
                    NavManager.NavigateTo(NavManager.Uri, forceLoad: true);
                }
            }
        }
    }
}