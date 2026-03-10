using System.Threading.Tasks;
using DevInstance.BlazorToolkit.Services;
using DevInstance.BlazorToolkit.Tools;
using DevInstance.DevCoreApp.Server.Admin.Services.Settings;
using DevInstance.LogScope;
using DevInstance.WebServiceToolkit.Exceptions;

namespace DevInstance.DevCoreApp.Server.Admin.Services.Appearance;

[BlazorService]
public class ThemeService : IThemeService
{
    private const string Category = "Appearance";
    private const string Key = "Theme";
    private const string DefaultTheme = "System";

    private static readonly HashSet<string> ValidThemes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Light", "Dark", "System"
    };

    private readonly ISettingsService _settingsService;
    private readonly IScopeLog _log;

    public ThemeService(IScopeManager logManager, ISettingsService settingsService)
    {
        _log = logManager.CreateLogger(this);
        _settingsService = settingsService;
    }

    public async Task<ServiceActionResult<string>> GetUserThemeAsync()
    {
        using var l = _log.TraceScope();

        var theme = await _settingsService.GetAsync<string>(Category, Key);
        var resolved = string.IsNullOrEmpty(theme) ? DefaultTheme : theme;

        l.I($"User theme preference: {resolved}");
        return ServiceActionResult<string>.OK(resolved);
    }

    public async Task<ServiceActionResult<string>> SetUserThemeAsync(string theme)
    {
        using var l = _log.TraceScope();

        if (string.IsNullOrWhiteSpace(theme) || !ValidThemes.Contains(theme))
        {
            throw new BadRequestException($"Invalid theme '{theme}'. Valid values: Light, Dark, System.");
        }

        // Normalize casing
        var normalized = ValidThemes.First(t => t.Equals(theme, StringComparison.OrdinalIgnoreCase));

        await _settingsService.SetAsync(Category, Key, normalized);

        l.I($"User theme set to: {normalized}");
        return ServiceActionResult<string>.OK(normalized);
    }
}
