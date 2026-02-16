using DevInstance.BlazorToolkit.Services;
using DevInstance.DevCoreApp.Server.Admin.Services.Email;
using DevInstance.DevCoreApp.Shared.Model;
using Microsoft.AspNetCore.Components;

namespace DevInstance.DevCoreApp.Server.Admin.WebService.UI.Pages.Admin;

public partial class EmailLogDetail
{
    [Parameter]
    public string Id { get; set; } = string.Empty;

    [Inject]
    private IEmailLogService EmailLogService { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [CascadingParameter]
    private IServiceExecutionHost Host { get; set; } = default!;

    private EmailLogItem? EmailLogEntry { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await LoadEmailLog();
    }

    private async Task LoadEmailLog()
    {
        await Host.ServiceReadAsync(
            async () => await EmailLogService.GetByIdAsync(Id),
            (result) => EmailLogEntry = result
        );
    }

    private async Task OnResend()
    {
        await Host.ServiceSubmitAsync(
            async () => await EmailLogService.ResendAsync(Id)
        );

        await LoadEmailLog();
    }

    private async Task OnDelete()
    {
        await Host.ServiceSubmitAsync(
            async () => await EmailLogService.DeleteAsync(Id)
        );

        NavigationManager.NavigateTo("admin/email-log");
    }

    private string GetStatusBadgeClass()
    {
        return EmailLogEntry?.Status switch
        {
            "Sent" => "bg-success",
            "Failed" => "bg-danger",
            "Batched" => "bg-warning text-dark",
            _ => "bg-secondary"
        };
    }
}
