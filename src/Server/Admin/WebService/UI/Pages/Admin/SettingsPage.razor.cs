using DevInstance.BlazorToolkit.Services;
using DevInstance.DevCoreApp.Server.Admin.Services.Settings;
using DevInstance.DevCoreApp.Shared.Model.Settings;
using Microsoft.AspNetCore.Components;

namespace DevInstance.DevCoreApp.Server.Admin.WebService.UI.Pages.Admin;

public partial class SettingsPage
{
    private const string ScopeSystem = "System";
    private const string ScopeTenant = "Tenant";
    private const string ScopeOrganization = "Organization";

    [Inject]
    private ISettingsAdminService SettingsAdminService { get; set; } = default!;

    [CascadingParameter]
    private IServiceExecutionHost Host { get; set; } = default!;

    private List<SettingItem>? Settings { get; set; }
    private IEnumerable<IGrouping<string, SettingItem>> GroupedSettings =>
        Settings?.GroupBy(s => s.Category).OrderBy(g => g.Key) ?? Enumerable.Empty<IGrouping<string, SettingItem>>();

    private string CurrentScope { get; set; } = ScopeSystem;
    private string SearchTerm { get; set; } = string.Empty;

    // Edit state
    private string? EditingSettingId { get; set; }
    private string EditingValue { get; set; } = string.Empty;

    // Reveal state for sensitive values
    private HashSet<string> RevealedSettings { get; set; } = new();

    // Add new setting form
    private bool ShowAddForm { get; set; }
    private SettingItem NewSetting { get; set; } = new();

    protected override async Task OnInitializedAsync()
    {
        await LoadSettings();
    }

    private async Task LoadSettings()
    {
        await Host.ServiceReadAsync(
            async () => await SettingsAdminService.GetAllByScopeAsync(CurrentScope, null, string.IsNullOrEmpty(SearchTerm) ? null : SearchTerm),
            (result) => Settings = result
        );
    }

    private Task OnScopeSystem() => OnScopeChanged(ScopeSystem);
    private Task OnScopeTenant() => OnScopeChanged(ScopeTenant);
    private Task OnScopeOrganization() => OnScopeChanged(ScopeOrganization);

    private async Task OnScopeChanged(string scope)
    {
        CurrentScope = scope;
        EditingSettingId = null;
        RevealedSettings.Clear();
        ShowAddForm = false;
        await LoadSettings();
    }

    private async Task OnSearch()
    {
        EditingSettingId = null;
        await LoadSettings();
    }

    private void OnStartEdit(SettingItem setting)
    {
        EditingSettingId = setting.Id;
        EditingValue = setting.IsSensitive && !RevealedSettings.Contains(setting.Id)
            ? string.Empty
            : setting.Value;
    }

    private void OnCancelEdit()
    {
        EditingSettingId = null;
        EditingValue = string.Empty;
    }

    private async Task OnSaveEdit()
    {
        if (EditingSettingId == null) return;

        await Host.ServiceSubmitAsync(
            async () => await SettingsAdminService.UpdateSettingAsync(EditingSettingId, EditingValue)
        );

        EditingSettingId = null;
        EditingValue = string.Empty;
        await LoadSettings();
    }

    private bool IsEditingTrue() => EditingValue == "true";

    private void OnEditBoolChanged(ChangeEventArgs e)
    {
        EditingValue = e.Value is true ? "true" : "false";
    }

    private void OnNewSettingBoolChanged(ChangeEventArgs e)
    {
        NewSetting.Value = e.Value is true ? "true" : "false";
    }

    private void OnRevealToggle(SettingItem setting)
    {
        if (RevealedSettings.Contains(setting.Id))
        {
            RevealedSettings.Remove(setting.Id);
        }
        else
        {
            RevealedSettings.Add(setting.Id);
        }
    }

    private void OnAddNew()
    {
        ShowAddForm = true;
        NewSetting = new SettingItem
        {
            Scope = CurrentScope,
            ValueType = "string",
            Value = string.Empty
        };
    }

    private void OnCancelAdd()
    {
        ShowAddForm = false;
        NewSetting = new();
    }

    private async Task OnCreateSetting()
    {
        NewSetting.Scope = CurrentScope;

        await Host.ServiceSubmitAsync(
            async () => await SettingsAdminService.CreateSettingAsync(NewSetting)
        );

        ShowAddForm = false;
        NewSetting = new();
        await LoadSettings();
    }

    private async Task OnDelete(SettingItem setting)
    {
        await Host.ServiceSubmitAsync(
            async () => await SettingsAdminService.DeleteSettingAsync(setting.Id)
        );

        await LoadSettings();
    }

    private static string TruncateValue(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        return value.Length <= maxLength ? value : value[..maxLength] + "...";
    }
}
