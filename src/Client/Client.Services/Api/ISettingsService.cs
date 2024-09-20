using Microsoft.Extensions.Localization;
using System.Globalization;

namespace DevInstance.DevCoreApp.Client.Services.Api;

public class SettingsLanguageItem
{
    public CultureInfo Culture { get; set; }
    public string DisplayName { get; set; }
    public string FlagName { get; set; }
}

public interface ISettingsService
{
    SettingsLanguageItem[] SupportedLanguages { get; }
}