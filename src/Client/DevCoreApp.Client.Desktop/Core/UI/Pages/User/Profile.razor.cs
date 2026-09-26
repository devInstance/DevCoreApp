using DevInstance.BlazorToolkit.Services;
using DevInstance.DevCoreApp.Client.Services.Core.Auth;
using DevInstance.DevCoreApp.Client.Services.Core.Me;
using DevInstance.DevCoreApp.Client.Services.Core.Users;
using DevInstance.DevCoreApp.Shared.Model.Core;
using Microsoft.AspNetCore.Components;

namespace DevInstance.DevCoreApp.Client.Desktop.Core.UI.Pages.User;

public partial class Profile
{
    [Inject] private IMeService Me { get; set; } = default!;
    [Inject] private IProfilePictureService Pictures { get; set; } = default!;
    [Inject] private ICurrentUserState CurrentUser { get; set; } = default!;

    [CascadingParameter] private IServiceExecutionHost Host { get; set; } = default!;

    private UserProfileItem? Input { get; set; }
    private string? pictureUrl;
    private string? successMessage;

    private static readonly IReadOnlyList<TimeZoneInfo> TimeZones = TimeZoneInfo.GetSystemTimeZones();
    private static string BrowserZone => TimeZoneInfo.Local.DisplayName;

    protected override async Task OnInitializedAsync()
    {
        await Host.ServiceReadAsync(
            async () => await Me.GetAsync(),
            result => Input = result.Profile);

        if (Input?.HasProfilePicture == true)
        {
            var picture = await Pictures.GetDataUrlAsync(Input.Id);
            pictureUrl = picture.Success ? picture.Result : null;
        }
    }

    private async Task UpdateProfileAsync()
    {
        if (Input == null) return;
        successMessage = null;

        await Host.ServiceSubmitAsync(
            async () => await Me.UpdateProfileAsync(Input),
            result =>
            {
                Input = result;
                successMessage = "Profile updated successfully.";
                // Re-read api/me so the new name and time zone apply across the app.
                CurrentUser.Reload();
            });
    }

    private string Initials
    {
        get
        {
            var first = Input?.FirstName;
            var last = Input?.LastName;
            var initials = $"{(string.IsNullOrWhiteSpace(first) ? "" : first[0])}{(string.IsNullOrWhiteSpace(last) ? "" : last[0])}";
            return initials.Length == 0 ? "?" : initials.ToUpperInvariant();
        }
    }
}
