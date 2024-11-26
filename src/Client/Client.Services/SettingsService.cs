using Microsoft.Extensions.Localization;
using System.Globalization;
using DevInstance.DevCoreApp.Shared.Services;
using DevInstance.BlazorToolkit.Services.Wasm;

namespace DevInstance.DevCoreApp.Client.Services;

public class SettingsService : BaseService, ISettingsService
{
    SettingsLanguageItem[] supportedLanguages;

    public SettingsLanguageItem[] SupportedLanguages { get => supportedLanguages; }

    public IStringLocalizer<ISettingsService> Loc { get; }

    public SettingsService(IStringLocalizer<ISettingsService> loc)
    {
        Loc = loc;
        supportedLanguages = new[]
        {
            CreateLanguageItem("en-US", "\U0001f1fa\U0001f1f8", loc),
            CreateLanguageItem("de-DE", "\U0001f1e9\U0001f1ea", loc),
            CreateLanguageItem("uk-UA", "\U0001f1fa\U0001f1e6", loc),
        };
    }

    private SettingsLanguageItem CreateLanguageItem(string culture, string flagName, IStringLocalizer<ISettingsService> loc)
    {
        return new SettingsLanguageItem
        {
            Culture = new CultureInfo(culture),
            DisplayName = loc[culture],
            FlagName = flagName
        };
    }
}