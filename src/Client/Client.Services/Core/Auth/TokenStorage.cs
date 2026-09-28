using System.Text.Json;
using Microsoft.JSInterop;

namespace DevInstance.DevCoreApp.Client.Services.Core.Auth;

/// <summary>Persists the session across page reloads.</summary>
public interface ITokenStorage
{
    Task<AuthTokens?> LoadAsync();
    Task SaveAsync(AuthTokens tokens);
    Task ClearAsync();
}

/// <summary>
/// Browser <c>localStorage</c>. Chosen with JWT for both clients (docs/WasmMigrationPlan.md, D3):
/// tokens there are readable by script on the page, which the strict Content-Security-Policy and
/// short access-token lifetime are meant to contain.
/// </summary>
public sealed class BrowserTokenStorage : ITokenStorage
{
    private const string Key = "devcoreapp.auth";
    private readonly IJSRuntime js;

    public BrowserTokenStorage(IJSRuntime js) => this.js = js;

    public async Task<AuthTokens?> LoadAsync()
    {
        try
        {
            var json = await js.InvokeAsync<string?>("localStorage.getItem", Key);
            return string.IsNullOrEmpty(json) ? null : JsonSerializer.Deserialize<AuthTokens>(json);
        }
        catch (JsonException)
        {
            // Corrupt or from an older shape: treat as signed out.
            return null;
        }
    }

    public async Task SaveAsync(AuthTokens tokens) =>
        await js.InvokeVoidAsync("localStorage.setItem", Key, JsonSerializer.Serialize(tokens));

    public async Task ClearAsync() =>
        await js.InvokeVoidAsync("localStorage.removeItem", Key);
}
