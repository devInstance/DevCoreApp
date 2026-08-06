namespace DevInstance.DevCoreApp.Server.Admin.Services.Core.Settings;

public interface ISettingsCacheInvalidator
{
    void Invalidate(string category, string key);
}
