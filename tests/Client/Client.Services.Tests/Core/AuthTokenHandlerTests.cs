using System.Net;
using System.Net.Http.Json;
using DevInstance.DevCoreApp.Client.Services.Core.Api;
using DevInstance.DevCoreApp.Client.Services.Core.Auth;
using DevInstance.DevCoreApp.Shared.Model.Core.Authentication;
using Xunit;

namespace DevInstance.DevCoreApp.Client.Services.Tests.Core;

public class AuthTokenHandlerTests
{
    private sealed class MemoryStorage : ITokenStorage
    {
        public AuthTokens? Saved;
        public Task<AuthTokens?> LoadAsync() => Task.FromResult(Saved);
        public Task SaveAsync(AuthTokens tokens) { Saved = tokens; return Task.CompletedTask; }
        public Task ClearAsync() { Saved = null; return Task.CompletedTask; }
    }

    /// <summary>Stands in for the server: answers with a function of the request.</summary>
    private sealed class FakeServer : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> respond;
        public readonly List<string> Calls = new();

        public FakeServer(Func<HttpRequestMessage, Task<HttpResponseMessage>> respond) => this.respond = respond;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            lock (Calls)
            {
                Calls.Add($"{request.RequestUri!.AbsolutePath} {request.Headers.Authorization?.Parameter}");
            }
            return await respond(request);
        }
    }

    private sealed class Factory : IHttpClientFactory
    {
        private readonly HttpMessageHandler handler;
        public Factory(HttpMessageHandler handler) => this.handler = handler;
        public HttpClient CreateClient(string name) =>
            new(handler, disposeHandler: false) { BaseAddress = new Uri("http://api.test/") };
    }

    private static readonly DateTime Later = DateTime.UtcNow.AddMinutes(15);

    private static (HttpClient Api, AuthTokenStore Store, MemoryStorage Storage, FakeServer Auth, FakeServer Server) Create(
        AuthTokens? tokens,
        Func<HttpRequestMessage, Task<HttpResponseMessage>> api,
        Func<HttpRequestMessage, Task<HttpResponseMessage>>? refresh = null)
    {
        var storage = new MemoryStorage { Saved = tokens };
        var store = new AuthTokenStore(storage);
        var auth = new FakeServer(refresh ?? (_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized))));
        var refresher = new TokenRefresher(new Factory(auth), store);
        var server = new FakeServer(api);
        var handler = new AuthTokenHandler(store, refresher) { InnerHandler = server };
        return (new HttpClient(handler) { BaseAddress = new Uri("http://api.test/") }, store, storage, auth, server);
    }

    private static HttpResponseMessage Json(object body) => new(HttpStatusCode.OK) { Content = JsonContent.Create(body) };

    private static Task<HttpResponseMessage> RefreshTo(string access, string refresh) =>
        Task.FromResult(Json(new JwtLoginResponse { Succeeded = true, AccessToken = access, RefreshToken = refresh, ExpiresAt = Later }));

    [Fact]
    public async Task attaches_bearer_token()
    {
        var (api, _, _, _, server) = Create(new AuthTokens("a1", "r1", Later), _ => Task.FromResult(Json("ok")));

        await api.GetAsync("api/me");

        Assert.Equal("/api/me a1", server.Calls.Single());
    }

    [Fact]
    public async Task sends_without_token_when_signed_out()
    {
        var (api, _, _, _, server) = Create(null, _ => Task.FromResult(Json("ok")));

        await api.GetAsync("api/me");

        Assert.Equal("/api/me ", server.Calls.Single());
    }

    [Fact]
    public async Task on_401_refreshes_once_and_replays_with_body()
    {
        string? replayedBody = null;
        var (api, store, storage, auth, server) = Create(
            new AuthTokens("a1", "r1", Later),
            async req =>
            {
                if (req.Headers.Authorization?.Parameter == "a1")
                    return new HttpResponseMessage(HttpStatusCode.Unauthorized);
                replayedBody = await req.Content!.ReadAsStringAsync();
                return Json("ok");
            },
            _ => RefreshTo("a2", "r2"));

        var response = await api.PostAsJsonAsync("api/me/theme", "Dark");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(new[] { "/api/me/theme a1", "/api/me/theme a2" }, server.Calls);
        Assert.Single(auth.Calls);
        Assert.Equal("\"Dark\"", replayedBody);
        Assert.Equal("r2", storage.Saved!.RefreshToken);
        Assert.Equal("a2", store.Current!.AccessToken);
    }

    [Fact]
    public async Task rejected_refresh_signs_out_and_returns_the_401()
    {
        var signedOut = false;
        var (api, store, storage, _, server) = Create(
            new AuthTokens("a1", "r1", Later),
            _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized)));
        await store.GetAsync();
        store.SignedInChanged += () => signedOut = true;

        var response = await api.GetAsync("api/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Single(server.Calls);
        Assert.Null(store.Current);
        Assert.Null(storage.Saved);
        Assert.True(signedOut);
    }

    [Fact]
    public async Task expired_token_is_refreshed_before_sending()
    {
        var (api, _, _, auth, server) = Create(
            new AuthTokens("a1", "r1", DateTime.UtcNow.AddSeconds(-5)),
            _ => Task.FromResult(Json("ok")),
            _ => RefreshTo("a2", "r2"));

        await api.GetAsync("api/me");

        Assert.Single(auth.Calls);
        Assert.Equal("/api/me a2", server.Calls.Single());
    }

    [Fact]
    public async Task concurrent_401s_share_a_single_refresh()
    {
        var refreshGate = new TaskCompletionSource();
        var (api, _, _, auth, _) = Create(
            new AuthTokens("a1", "r1", Later),
            req => Task.FromResult(req.Headers.Authorization?.Parameter == "a1"
                ? new HttpResponseMessage(HttpStatusCode.Unauthorized)
                : Json("ok")),
            async _ => { await refreshGate.Task; return await RefreshTo("a2", "r2"); });

        var calls = Enumerable.Range(0, 5).Select(_ => api.GetAsync("api/me")).ToArray();
        await Task.Delay(50);
        refreshGate.SetResult();
        var responses = await Task.WhenAll(calls);

        Assert.All(responses, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));
        Assert.Single(auth.Calls);
    }
}
