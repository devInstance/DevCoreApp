using Microsoft.Extensions.Localization;
using System.Globalization;

namespace DevInstance.DevCoreApp.Client.Services
{
    public class SettingsService : BaseService
    {
        public class LanguageItem
        {
            public CultureInfo Culture { get; set; }
            public string DisplayName { get; set; }
            public string FlagName { get; set; }
        }

        LanguageItem[] supportedLanguages;

        public LanguageItem[] SupportedLanguages { get => supportedLanguages; }
        public IStringLocalizer<SettingsService> Loc { get; }

        public SettingsService(IStringLocalizer<SettingsService> loc)
        {
            Loc = loc;
            supportedLanguages = new[]
            {
                CreateLanguageItem("en-US", "\U0001f1fa\U0001f1f8", loc),
                CreateLanguageItem("de-DE", "\U0001f1e9\U0001f1ea", loc),
                CreateLanguageItem("uk-UA", "\U0001f1fa\U0001f1e6", loc),
            };
        }

        private LanguageItem CreateLanguageItem(string culture, string flagName, IStringLocalizer<SettingsService> loc)
        {
            return new LanguageItem
            {
                Culture = new CultureInfo(culture),
                DisplayName = loc[culture],
                FlagName = flagName
            };
        }

    }
}