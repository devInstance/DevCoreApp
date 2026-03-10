using System.Threading.Tasks;
using DevInstance.BlazorToolkit.Services;

namespace DevInstance.DevCoreApp.Server.Admin.Services.Appearance;

public interface IThemeService
{
    Task<ServiceActionResult<string>> GetUserThemeAsync();
    Task<ServiceActionResult<string>> SetUserThemeAsync(string theme);
}
