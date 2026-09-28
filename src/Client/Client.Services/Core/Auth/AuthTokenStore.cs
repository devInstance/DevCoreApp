namespace DevInstance.DevCoreApp.Client.Services.Core.Auth;

/// <summary>
/// The current session, shared app-wide. A singleton on purpose: <c>IHttpClientFactory</c> builds
/// message handlers in their own DI scope, so a scoped store would give
/// <see cref="AuthTokenHandler"/> a different instance than the pages see.
/// </summary>
public sealed class AuthTokenStore
{
    private readonly ITokenStorage storage;
    private readonly SemaphoreSlim loadGate = new(1, 1);
    private bool loaded;

    public AuthTokenStore(ITokenStorage storage) => this.storage = storage;

    public AuthTokens? Current { get; private set; }

    /// <summary>Raised on sign-in and sign-out — not on the silent token rotation of a refresh.</summary>
    public event Action? SignedInChanged;

    /// <summary>The current tokens, loading them from storage on first use.</summary>
    public async Task<AuthTokens?> GetAsync()
    {
        if (!loaded)
        {
            await loadGate.WaitAsync();
            try
            {
                if (!loaded)
                {
                    Current = await storage.LoadAsync();
                    loaded = true;
                }
            }
            finally
            {
                loadGate.Release();
            }
        }

        return Current;
    }

    /// <summary>Stores new tokens. <paramref name="signIn"/> false for a refresh (same user, new tokens).</summary>
    public async Task SetAsync(AuthTokens tokens, bool signIn)
    {
        Current = tokens;
        loaded = true;
        await storage.SaveAsync(tokens);
        if (signIn)
        {
            SignedInChanged?.Invoke();
        }
    }

    public async Task ClearAsync()
    {
        var wasSignedIn = Current != null;
        Current = null;
        loaded = true;
        await storage.ClearAsync();
        if (wasSignedIn)
        {
            SignedInChanged?.Invoke();
        }
    }
}
