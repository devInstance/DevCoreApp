namespace DevInstance.DevCoreApp.Server.Services.Core.Settings;

public interface ISettingsCacheInvalidator
{
    void Invalidate(string category, string key);
}
