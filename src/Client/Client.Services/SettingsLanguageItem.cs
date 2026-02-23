using System.Globalization;

namespace DevInstance.DevCoreApp.Client.Services
{
    public class SettingsLanguageItem
    {
        public CultureInfo Culture { get; set; }
        public string DisplayName { get; set; }
        public string FlagName { get; set; }
    }
}