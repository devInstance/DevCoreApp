namespace DevInstance.DevCoreApp.Server.Admin.Services.Settings;

public interface ISettingsCacheInvalidator
{
    void Invalidate(string category, string key);
}
