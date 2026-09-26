using DevInstance.DevCoreApp.Client.Services.Core.Users;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace DevInstance.DevCoreApp.Client.Desktop.Core.UI.Components;

public partial class ProfilePictureUpload
{
    [Parameter] public string? PictureUrl { get; set; }
    [Parameter] public string? FirstName { get; set; }
    [Parameter] public string? LastName { get; set; }
    [Parameter] public bool AllowEdit { get; set; }
    [Parameter] public string Size { get; set; } = "large";
    [Parameter] public EventCallback<IBrowserFile> OnUpload { get; set; }
    [Parameter] public EventCallback OnDelete { get; set; }

    [Inject] private IProfilePictureService Pictures { get; set; } = default!;

    private bool IsUploading { get; set; }
    private string? _errorMessage;

    // PictureUrl is an API path that needs the bearer token, which <img src> never sends, so the
    // image is fetched through the service and shown as a data: URL.
    private string? _imageSrc;
    private string? _loadedUrl;

    protected override async Task OnParametersSetAsync()
    {
        if (PictureUrl == _loadedUrl)
        {
            return;
        }

        _loadedUrl = PictureUrl;
        if (string.IsNullOrEmpty(PictureUrl))
        {
            _imageSrc = null;
            return;
        }

        var result = await Pictures.GetDataUrlFromPathAsync(PictureUrl);
        _imageSrc = result.Success ? result.Result : null;
    }

    private string Initials
    {
        get
        {
            var first = !string.IsNullOrEmpty(FirstName) ? FirstName[0].ToString().ToUpper() : "";
            var last = !string.IsNullOrEmpty(LastName) ? LastName[0].ToString().ToUpper() : "";
            return first + last;
        }
    }

    private string InitialsColor
    {
        get
        {
            var name = $"{FirstName}{LastName}";
            if (string.IsNullOrEmpty(name)) return "#6c757d";

            var hash = 0;
            foreach (var c in name)
                hash = c + ((hash << 5) - hash);

            var h = Math.Abs(hash % 360);
            return $"hsl({h}, 45%, 55%)";
        }
    }

    private string SizeClass => Size switch
    {
        "small" => "pp-small",
        "medium" => "pp-medium",
        _ => "pp-large"
    };

    private async Task OnFileSelected(InputFileChangeEventArgs e)
    {
        _errorMessage = null;

        var file = e.File;
        if (file.Size > 2 * 1024 * 1024)
        {
            _errorMessage = "Image must be less than 2 MB.";
            return;
        }

        var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp" };
        if (!allowedTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
        {
            _errorMessage = "Only JPEG, PNG, and WebP images are allowed.";
            return;
        }

        IsUploading = true;
        try
        {
            await OnUpload.InvokeAsync(file);
            // Same URL, new image: the service dropped its cached copy, so reload.
            _loadedUrl = null;
            await OnParametersSetAsync();
        }
        finally
        {
            IsUploading = false;
        }
    }

    private async Task OnRemoveClick()
    {
        _errorMessage = null;
        IsUploading = true;
        try
        {
            await OnDelete.InvokeAsync();
        }
        finally
        {
            IsUploading = false;
        }
    }
}
