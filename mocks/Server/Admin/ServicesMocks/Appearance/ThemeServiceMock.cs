using System.Threading.Tasks;
using DevInstance.BlazorToolkit.Services;
using DevInstance.BlazorToolkit.Tools;
using DevInstance.WebServiceToolkit.Exceptions;

namespace DevInstance.DevCoreApp.Server.Admin.Services.Appearance;

[BlazorServiceMock]
public class ThemeServiceMock : IThemeService
{
    private static readonly HashSet<string> ValidThemes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Light", "Dark", "System"
    };

    private string _theme = "System";
    private readonly int _delay = 100;

    public async Task<ServiceActionResult<string>> GetUserThemeAsync()
    {
        await Task.Delay(_delay);
        return ServiceActionResult<string>.OK(_theme);
    }

    public async Task<ServiceActionResult<string>> SetUserThemeAsync(string theme)
    {
        await Task.Delay(_delay);

        if (string.IsNullOrWhiteSpace(theme) || !ValidThemes.Contains(theme))
        {
            throw new BadRequestException($"Invalid theme '{theme}'. Valid values: Light, Dark, System.");
        }

        _theme = ValidThemes.First(t => t.Equals(theme, StringComparison.OrdinalIgnoreCase));
        return ServiceActionResult<string>.OK(_theme);
    }
}
