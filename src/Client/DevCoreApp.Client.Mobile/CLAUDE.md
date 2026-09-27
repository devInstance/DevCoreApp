# CLAUDE.md — Mobile client (Blazor WebAssembly field app)

A small, phone-first WASM app. Today it has login, a home screen and the user's profile. It exists
to prove that `Client.Services` works for a second client, and it is the place where a fork's field
features go. It talks to the server only through `/api` (JWT), exactly like Desktop.

Also read: the root [`CLAUDE.md`](../../../CLAUDE.md) (Core/App split, naming), the wire contract
in [`docs/Api.md`](../../../docs/Api.md), and the Desktop [`CLAUDE.md`](../DevCoreApp.Client.Desktop/CLAUDE.md).
Pages, client services, dates and authorization follow the same rules as Desktop.

## Run

```bash
# Normal: run the Api host; it serves this app at /mobile/ (same origin, shares Desktop's sign-in)
dotnet run --project src/Server/Api/DevCoreApp.Server.Api.csproj

# Standalone dev server against a running Api host (launch environment "Standalone" →
# wwwroot/appsettings.Standalone.json ApiBaseUrl)
dotnet run --project src/Client/DevCoreApp.Client.Mobile/DevCoreApp.Client.Mobile.csproj   # http://localhost:5290
```

## Things that differ from Desktop

- **Base path `/mobile/`.**
  - `StaticWebAssetBasePath` is `mobile` in the `.csproj`, and `wwwroot/index.html` has
    `<base href="/mobile/">`. Keep them in sync.
  - Links and `NavigateTo` use **relative** URLs (`profile`, not `/profile`), so they resolve
    under the base.
  - The API base is the **origin** only (`Program.ResolveApiBase`), so API calls go to `/api/...`,
    not `/mobile/api/...`.
- **Shared session.** Both clients store tokens in the same origin's `localStorage`
  (`AuthTokenStore`). Signing in on one signs in the other.
- **No admin UI and no mocks build yet.** Add a `-c ServiceMocks` branch in `Program.cs` the same
  way Desktop does when mobile screens need it.
- **Styles are plain CSS** in `wwwroot/css` plus component `.razor.css`. There is no SCSS/TS
  pipeline, unlike Desktop.

## Adding field features

Product screens go under `App/UI/Pages`. Their client services go under
`Client.Services/App/<Feature>` (shared with Desktop) or, if they are mobile-only, under
`App/Services` here. Offline/caching (BlazorToolkit `CacheableSource`, IndexedDB) is product
code: keep it in `App`.
