namespace DevInstance.DevCoreApp.Client.Services.Mocks.Core;

/// <summary>
/// Role names for the fake data. Mirrors the server's ApplicationRoles, which the client
/// cannot reference; the WASM clients themselves only ever read role names from api/me.
/// </summary>
internal static class ApplicationRoles
{
    public const string Owner = "Owner";
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string Employee = "Employee";
    public const string Client = "Client";
}
