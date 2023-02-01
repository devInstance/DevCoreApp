using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Client.UI.Components
{
    public partial class WebPageTitle
    {
        [Inject]
        IStringLocalizer<WebPageTitle> loc { get; set; }

        [Inject]
        IJSRuntime JSRuntime { get; set; }

        [Parameter]
        public string Value { get; set; }

        [Parameter]
        public string AppName { get; set; }

        [Parameter]
        public string Format { get; set; }

        private string FullTitle { get; set; }

        protected override void OnInitialized()
        {
            AppName = loc["DevCoreApp"];
            Format = loc["{1} · {0}"];
        }

        protected override void OnParametersSet()
        {
            base.OnParametersSet();

            if (string.IsNullOrEmpty(Value))
            {
                FullTitle = AppName;
            }
            else
            {
                FullTitle = string.Format(Format, AppName, Value);
            }
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await JSRuntime.InvokeVoidAsync("blazor_setTitle", FullTitle);
        }
    }
}
