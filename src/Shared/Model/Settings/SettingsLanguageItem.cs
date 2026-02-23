using System.Globalization;

namespace DevInstance.DevCoreApp.Shared.Model.Settings
{
    public class SettingsLanguageItem
    {
        public CultureInfo Culture { get; set; }
        public string DisplayName { get; set; }
        public string FlagName { get; set; }
    }
}