using Bogus;
using DevInstance.BlazorToolkit.Services;
using DevInstance.BlazorToolkit.Tools;
using DevInstance.DevCoreApp.Server.Admin.Services.Settings;
using DevInstance.DevCoreApp.Shared.Model.Settings;
using DevInstance.WebServiceToolkit.Common.Tools;

namespace DevInstance.DevCoreApp.Server.Admin.Services.Mocks.Settings;

[BlazorServiceMock]
public class SettingsAdminServiceMock : ISettingsAdminService
{
    private readonly List<SettingItem> _settings = new();
    private int delay = 500;

    public SettingsAdminServiceMock()
    {
        _settings = GenerateSettings();
    }

    private static List<SettingItem> GenerateSettings()
    {
        var settings = new List<SettingItem>();

        // System settings
        settings.AddRange(new[]
        {
            new SettingItem { Id = Guid.NewGuid().ToString(), Category = "General", Key = "ApplicationName", Value = "DevCoreApp", ValueType = "string", Description = "Display name of the application", Scope = "System" },
            new SettingItem { Id = Guid.NewGuid().ToString(), Category = "General", Key = "MaintenanceMode", Value = "false", ValueType = "bool", Description = "When enabled, the application shows a maintenance page", Scope = "System" },
            new SettingItem { Id = Guid.NewGuid().ToString(), Category = "General", Key = "MaxUploadSizeMB", Value = "25", ValueType = "int", Description = "Maximum file upload size in megabytes", Scope = "System" },
            new SettingItem { Id = Guid.NewGuid().ToString(), Category = "General", Key = "DefaultLanguage", Value = "en", ValueType = "string", Description = "Default language for the application", Scope = "System" },
            new SettingItem { Id = Guid.NewGuid().ToString(), Category = "Email", Key = "SmtpHost", Value = "smtp.example.com", ValueType = "string", Description = "SMTP server hostname", Scope = "System" },
            new SettingItem { Id = Guid.NewGuid().ToString(), Category = "Email", Key = "SmtpPort", Value = "587", ValueType = "int", Description = "SMTP server port", Scope = "System" },
            new SettingItem { Id = Guid.NewGuid().ToString(), Category = "Email", Key = "SmtpUsername", Value = "noreply@example.com", ValueType = "string", Description = "SMTP authentication username", Scope = "System" },
            new SettingItem { Id = Guid.NewGuid().ToString(), Category = "Email", Key = "SmtpPassword", Value = "s3cr3t-p@ssw0rd", ValueType = "string", Description = "SMTP authentication password", IsSensitive = true, Scope = "System" },
            new SettingItem { Id = Guid.NewGuid().ToString(), Category = "Email", Key = "FromAddress", Value = "noreply@example.com", ValueType = "string", Description = "Default sender email address", Scope = "System" },
            new SettingItem { Id = Guid.NewGuid().ToString(), Category = "Email", Key = "EnableEmailSending", Value = "true", ValueType = "bool", Description = "Enable or disable email sending globally", Scope = "System" },
            new SettingItem { Id = Guid.NewGuid().ToString(), Category = "Security", Key = "PasswordMinLength", Value = "8", ValueType = "int", Description = "Minimum password length", Scope = "System" },
            new SettingItem { Id = Guid.NewGuid().ToString(), Category = "Security", Key = "RequireTwoFactor", Value = "false", ValueType = "bool", Description = "Require two-factor authentication for all users", Scope = "System" },
            new SettingItem { Id = Guid.NewGuid().ToString(), Category = "Security", Key = "SessionTimeoutMinutes", Value = "30", ValueType = "int", Description = "Session timeout in minutes", Scope = "System" },
            new SettingItem { Id = Guid.NewGuid().ToString(), Category = "Security", Key = "JwtSecret", Value = "super-secret-jwt-key-256", ValueType = "string", Description = "JWT signing secret key", IsSensitive = true, Scope = "System" },
            new SettingItem { Id = Guid.NewGuid().ToString(), Category = "Jobs", Key = "MaxConcurrentJobs", Value = "5", ValueType = "int", Description = "Maximum number of concurrent background jobs", Scope = "System" },
            new SettingItem { Id = Guid.NewGuid().ToString(), Category = "Jobs", Key = "RetryDelaySeconds", Value = "60", ValueType = "int", Description = "Delay between job retries in seconds", Scope = "System" },
            new SettingItem { Id = Guid.NewGuid().ToString(), Category = "Jobs", Key = "JobConfiguration", Value = "{\"cleanupIntervalHours\":24,\"maxRetries\":3,\"deadLetterEnabled\":true}", ValueType = "json", Description = "Advanced job processing configuration", Scope = "System" },
            new SettingItem { Id = Guid.NewGuid().ToString(), Category = "Storage", Key = "Provider", Value = "Local", ValueType = "string", Description = "File storage provider (Local, AzureBlob, S3)", Scope = "System" },
            new SettingItem { Id = Guid.NewGuid().ToString(), Category = "Storage", Key = "ConnectionString", Value = "DefaultEndpointsProtocol=https;AccountName=devcore", ValueType = "string", Description = "Storage provider connection string", IsSensitive = true, Scope = "System" },
        });

        // Tenant settings
        settings.AddRange(new[]
        {
            new SettingItem { Id = Guid.NewGuid().ToString(), Category = "Branding", Key = "CompanyName", Value = "Acme Corporation", ValueType = "string", Description = "Tenant company name for branding", Scope = "Tenant" },
            new SettingItem { Id = Guid.NewGuid().ToString(), Category = "Branding", Key = "PrimaryColor", Value = "#0d6efd", ValueType = "string", Description = "Primary brand color (hex)", Scope = "Tenant" },
            new SettingItem { Id = Guid.NewGuid().ToString(), Category = "Branding", Key = "LogoUrl", Value = "/images/logo.png", ValueType = "string", Description = "URL to the tenant logo", Scope = "Tenant" },
            new SettingItem { Id = Guid.NewGuid().ToString(), Category = "Features", Key = "EnableNotifications", Value = "true", ValueType = "bool", Description = "Enable in-app notifications for this tenant", Scope = "Tenant" },
            new SettingItem { Id = Guid.NewGuid().ToString(), Category = "Features", Key = "MaxUsersAllowed", Value = "50", ValueType = "int", Description = "Maximum number of users allowed for the tenant", Scope = "Tenant" },
        });

        // Organization settings
        var orgId1 = Guid.NewGuid().ToString();
        var orgId2 = Guid.NewGuid().ToString();
        settings.AddRange(new[]
        {
            new SettingItem { Id = Guid.NewGuid().ToString(), Category = "General", Key = "DefaultLanguage", Value = "fr", ValueType = "string", Description = "Default language for the organization", Scope = "Organization", OrganizationId = orgId1, OrganizationName = "East Region" },
            new SettingItem { Id = Guid.NewGuid().ToString(), Category = "General", Key = "TimeZone", Value = "America/New_York", ValueType = "string", Description = "Default timezone for the organization", Scope = "Organization", OrganizationId = orgId1, OrganizationName = "East Region" },
            new SettingItem { Id = Guid.NewGuid().ToString(), Category = "General", Key = "DefaultLanguage", Value = "es", ValueType = "string", Description = "Default language for the organization", Scope = "Organization", OrganizationId = orgId2, OrganizationName = "West Region" },
            new SettingItem { Id = Guid.NewGuid().ToString(), Category = "Features", Key = "EnableReports", Value = "true", ValueType = "bool", Description = "Enable reporting module for this organization", Scope = "Organization", OrganizationId = orgId1, OrganizationName = "East Region" },
        });

        return settings;
    }

    public async Task<ServiceActionResult<List<SettingItem>>> GetAllByScopeAsync(
        string scope, string? organizationId = null, string? search = null)
    {
        await Task.Delay(delay);

        var filtered = _settings.Where(s => s.Scope == scope);

        if (scope == "Organization" && !string.IsNullOrEmpty(organizationId))
        {
            filtered = filtered.Where(s => s.OrganizationId == organizationId);
        }

        if (!string.IsNullOrEmpty(search))
        {
            filtered = filtered.Where(s =>
                s.Category.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                s.Key.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (s.Description != null && s.Description.Contains(search, StringComparison.OrdinalIgnoreCase)));
        }

        var items = filtered.OrderBy(s => s.Category).ThenBy(s => s.Key).ToList();

        return ServiceActionResult<List<SettingItem>>.OK(items);
    }

    public async Task<ServiceActionResult<SettingItem>> UpdateSettingAsync(string id, string newValue)
    {
        await Task.Delay(delay);

        var item = _settings.Find(s => s.Id == id);
        if (item == null) throw new InvalidOperationException("Setting not found.");

        item.Value = newValue;

        return ServiceActionResult<SettingItem>.OK(item);
    }

    public async Task<ServiceActionResult<SettingItem>> CreateSettingAsync(SettingItem item)
    {
        await Task.Delay(delay);

        item.Id = Guid.NewGuid().ToString();
        _settings.Add(item);

        return ServiceActionResult<SettingItem>.OK(item);
    }

    public async Task<ServiceActionResult<bool>> DeleteSettingAsync(string id)
    {
        await Task.Delay(delay);

        var item = _settings.Find(s => s.Id == id);
        if (item == null) throw new InvalidOperationException("Setting not found.");

        _settings.Remove(item);

        return ServiceActionResult<bool>.OK(true);
    }
}
