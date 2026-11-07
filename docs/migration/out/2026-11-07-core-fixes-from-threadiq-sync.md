---
origin: DevCoreApp
targets: [ThreadIQ, Tentrie]
scope:
  - Server.Services.Core.Authentication.PermissionClaimsTransformation   # API key = scopes ∩ owner's permissions
  - Server.Services.Core.ApiKeys.ApiKeyPermissionSnapshotService          # overrides keyed by Identity user id
  - Server.Services.Core.Files.FileService                                # DownloadAsync / GetUrlAsync skip soft-deleted
  - Server.Services.Core.Background.BackgroundWorker                      # async DI scope
  - Server.Services.Core.Background.Tasks.BackgroundTaskWorker            # async DI scope (3 sites)
  - Server.Api.Core.Identity.ApiKeyAuthenticationHandler                  # async DI scope in the LastUsedAt touch
  - Server.Api.Core.Controllers.ApiControllerBase                         # logger passed to HandleWebRequest*
  - Server.Api.Core.Controllers.ApiConfigurationExtensions                # StringOutputFormatter removed
  - Server.Api.Core.Controllers.FileController                            # DownloadAsync goes through Unwrap
  - Client.Services.Core.ImportExport.IImportExportService / ImportExportService   # + DownloadTemplateAsync
  - Client.Desktop.Core.UI.Pages.Admin.ImportPage                         # template buttons fetch through the API client
  - Client.Services.Mocks.Core.ImportExport.ImportExportServiceMock       # + DownloadTemplateAsync
  - Client.Desktop / Client.Mobile project files                          # Release: drop appsettings.Development.json; tsc optional
  - tests Server.Api Core.Api.ApiContractTests                            # + string_result_is_json_string_not_plain_text
  - Server.Services.Core.Settings.SettingsService                         # per-operation repository (item 10)
  - Server.Services.Core.FeatureFlags.FeatureFlagService                  # per-operation repository (item 10)
  - Server.Services.Core.Webhooks.WebhookDispatcher                       # per-operation repository (item 10)
  - Server.Services.Core.Roles.RoleManagementService                      # per-operation repository (item 10)
  - Server.Services.Core.Authentication.JwtAuthService                    # per-operation repository (item 10)
  - docs/ApiKeys.md                                                       # intersection semantics + known gaps
status: pending
related:
  - docs/migration/in/applied/2026-10-09-role-editor-page.md          # also fan out to Tentrie
  - docs/migration/in/applied/2026-10-20-search-highlight-ranges.md   # also fan out to Tentrie
  - docs/migration/in/applied/2026-11-02-csvhelper-34-port.md         # also fan out to Tentrie
  - docs/migration/out/2026-11-07-threadiq-catchup-devcoreapp-ahead.md
---

# Core fixes collected from the 2026-11-07 ThreadIQ sync

## Delivery & audit log

| Date | Event |
|---|---|
| 2026-11-07 | Authored in DevCoreApp `out/` after a file-by-file diff of ThreadIQ `main` (`13310e5`) against DevCoreApp's shared surface. Not yet delivered. |

## Why

A full comparison of ThreadIQ's shared files with DevCoreApp found Core bug fixes that ThreadIQ
made in passing (several under `#region project-specific ThreadIQ` fences) without an outbound
doc. They are generic, so DevCoreApp has adopted them as canonical Core. Two more bugs turned up
during the review and are fixed here as well. **ThreadIQ**: most items came from you. Replace your
fenced versions with the canonical code and **remove the fences**, since the change is no longer a
deviation. **Tentrie**: apply everything.

**Tentrie only:** also apply the three ThreadIQ docs that DevCoreApp applied on the same day.
They are listed under `related` and archived in DevCoreApp's `docs/migration/in/applied/` with
their deviation notes. Copy them into your `in/` as they are.

## Current state → change

| # | Surface | Bug | Change | ThreadIQ origin |
|---|---|---|---|---|
| 1 | `PermissionClaimsTransformation` | **Security.** An API-key principal got exactly `ApiKey.Scopes`. Creation (`ApiKeyAdminService.ResolveScopesAsync`) accepts any requested scope, so a user who can create keys could mint one with permissions they lack. A key also kept permissions its owner had since lost. | Compute the owner's effective permissions as for any user (roles + overrides; the API-key principal already carries the owner's role claims), then `permissionKeys.IntersectWith(apiKey.Scopes)`. One code path instead of a separate API-key branch. | `86c5215a`, fenced |
| 2 | `ApiKeyPermissionSnapshotService` | Overrides were looked up by **profile id**, but `UserPermissionOverride.UserId` is the **Identity user id**, so overrides never reached a key's default snapshot. Present in both repos. | `.ByUserId(applicationUserId.Value)` | new (both repos) |
| 3 | `FileService.DownloadAsync`, `GetUrlAsync` | Files soft-deleted with `Storage:SoftDelete` (`IsActive = false`) stayed downloadable. | `.Where(f => f.IsActive)` after `ByPublicId(...).Select()`. | `f1f1213a` (download only; `GetUrlAsync` is new) |
| 4 | `ImportPage` + client `IImportExportService` | The template links were plain `<a href="api/import-export/template…">`. WASM sends the JWT only in a header, so the authorized endpoint always answered 401. | `DownloadTemplateAsync(entityType, format)` on the client service (GET through `Http`, returns `ExportDownloadResult`). The buttons call it and hand the bytes to `downloadFileFromBytes`. Mock added. | `5034f88`, fenced |
| 5 | `ApiConfigurationExtensions` | An `ActionResult<string>` went out as `text/plain` (`Kanban`). The clients parse every success body as JSON. | `options.OutputFormatters.RemoveType<StringOutputFormatter>();` plus a contract test. | `32b7250` |
| 6 | `ApiControllerBase` | `HandleWebRequest*` catches exceptions itself, so they never reach `ApiExceptionHandler`. Without a logger, a 500 left no log entry. | Pass `CreateLog()` (`HttpContext?.RequestServices.GetService<IScopeManager>()?.CreateLogger(this)`) as the second argument. | `1e024e9` |
| 7 | `FileController.DownloadAsync` | Read `result.Result` directly; a failed result would dereference null and return 500. | `var download = Unwrap(await _fileService.DownloadAsync(id));` | `5f50fb7` |
| 8 | `BackgroundWorker`, `BackgroundTaskWorker` (3), `ApiKeyAuthenticationHandler` | A synchronous `CreateScope()` disposing an `IAsyncDisposable`-only service throws **after** the task has committed. The caller sees a failure and retries, so the job runs twice. | `await using var scope = _scopeFactory.CreateAsyncScope();` | `384e0b6` (workers); handler is new |
| 9 | Desktop + Mobile `.csproj` | A Release build saw `wwwroot/appsettings.Development.json` twice ("More than one compatible asset…") and would ship dev config. On a fresh CI checkout `CompileTypeScript` can fire with no `tsc` on the agent and fail the build. | `<ItemGroup Condition="'$(Configuration)' == 'Release'"><Content Remove="wwwroot\appsettings.Development.json" /></ItemGroup>`. Probe `tsc --version`; if it's absent, log a message and use the committed JS; fail only on a real compile error. | `daa5e01`, `bba1922` |
| 10 | `SettingsService`, `FeatureFlagService`, `WebhookDispatcher`, `RoleManagementService`, `JwtAuthService`, `ApiKeyPermissionSnapshotService` | These request-path services injected the **scoped** `IQueryRepository`, which breaks the per-operation unit-of-work rule (`UnitOfWork.md`). Settings and flag lookups run from inside other services' operations; `WebhookDispatcher` and the snapshot service are called in the middle of another service's operation. Concurrent use in one request hits "A second operation was started on this context instance". | Inject `IQueryRepositoryFactory`; open `await using var repo = _repositoryFactory.Create();` once per public method. Private helpers take `IQueryRepository repo` (`JwtAuthService`). `JwtAuthService.RefreshAsync` keeps the token lookup and the rotation on one `repo`, since a single `AddAsync` flushes both. `FeatureFlagService` keeps its "no factory → all flags off" mock behavior. Auth-pipeline classes (`PermissionClaimsTransformation`, `AuthorizationContext`, `OrganizationContextResolver`, `ApiKeyAuthenticationHandler`), health checks, background workers and seeders keep the scoped repository, as CLAUDE.md allows. | ThreadIQ `2c6af628` (#883) for the first three; the rest are new |

The exact code is in DevCoreApp's commit for this sync (fill in the SHA before delivering). The
diff is a reference, not a patch: namespace prefixes differ.

**Behavior change in item 1 (read before applying).** Before this fix, `docs/ApiKeys.md` described
stored scopes as a stable snapshot: "old integrations remain stable when the owner is promoted or
demoted". With the intersection, promotion still does not broaden a key, but **demotion now
narrows it**: a key stops working for anything its owner loses. That is the security intent. An
integration owned by a user who gets demoted will start receiving 403s; re-own the key under an
account that keeps the permission. `docs/ApiKeys.md` is updated to match.

## Affected shared surface — fences to reconcile

- **ThreadIQ:** fences around the old versions of items 1 and 4 (`PermissionClaimsTransformation`,
  `IImportExportService`, `ImportExportService`, `ImportPage.razor(.cs)`). Replace them with the
  canonical code and delete the fence. ThreadIQ's item 1 code queries `_db.ApiKeys` directly; the
  canonical version goes through `GetApiKeyQuery(null!)`.
- **Tentrie:** check for a local fence on any listed file before applying.

## Verification

- `dotnet build <solution>` in `Debug`, `ServiceMocks` and `Release`, then `dotnet test <solution>`.
  DevCoreApp: all three configurations build, 122/122 tests pass.
- API key: create a key as a user without `X`, request scope `X`, then call an `X`-protected endpoint
  with the key → 403. Remove a permission from the owner's role and the existing key loses it on the next request.
- Soft-delete a file with `Storage:SoftDelete` on → `GET api/files/{id}` returns 404.
- Admin → Import → pick an entity → the CSV/Excel Template buttons download a file (previously 401).

## Open questions

### API keys bypass their scopes outside permission-policy endpoints (unresolved, under study)

Item 1 closes the gap for endpoints guarded by `[Authorize(Policy = "<permission>")]`. A key's
scopes are enforced **only** through `Permission:*` claims, though, and three other checks don't
look at them. **Not changed here**; DevCoreApp is studying the problem first. Forks should not
invent a local fix; wait for the canonical one.

**How an API-key principal is built** (`Server.Api.Core.Identity.ApiKeyAuthenticationHandler`):
claims are `NameIdentifier` = the owner's Identity user id, `Name`, `ApiKeyId`, **plus every role
the owner holds** (`userManager.GetRolesAsync(appUser)` → `ClaimTypes.Role`). After that,
`PermissionClaimsTransformation` adds `Permission:*` (scopes ∩ owner's permissions, after item 1)
and the owner's organization claims.

**Gap A — role-gated endpoints.** `[Authorize(Roles = …)]` reads the role claims, which the key
inherits from its owner in full.
- `Server.Api.Core.Controllers.ImportExportController`: class-level
  `[Authorize(Roles = "Owner,Admin")]` over every action: import entity types/fields,
  `requires-organization`, parse-headers, validate, commit, rollback, session read, export
  entity types/fields, `POST export`, `GET template`. A key created by an Owner/Admin with, for
  example, only `Admin.Users.View` can still **export data and commit imports**.
- `Server.Services.Core.GridProfileService` (around line 124): a service-level
  `User.IsInRole(Owner|Admin|Manager)` check that decides who may save shared/default grid profiles.

**Gap B — authentication-only endpoints.** These have `[Authorize]` and no permission check, so any
valid key acts as its owner whatever its scopes:
- `FileController`: `POST upload`, `GET {id}/download`, `DELETE {id}`, `GET {id}/url`.
- `ProfilePictureController`: `POST`, `DELETE`, `GET`, `GET thumbnail` (the owner's own picture).
- `GridProfilesController`: `GET {gridName}`, `PUT`.
- `MeController`, `NotificationsController`, `NotificationHub` (`/hubs/notifications`).
- `AuthController` `POST revoke`.
Some may be fine for a key (reading its own `/api/me`); others, like file delete, probably are not.
"Any authenticated caller" was written with interactive users in mind.

**Gap C — `ApiKey.OrganizationId` is not enforced.** The key's organization is stored at creation
(`ApiKeyAdminService.ResolveOrganizationIdAsync`), but
`PermissionClaimsTransformation.AddOrganizationClaimsAsync` → `OrganizationContextResolver.ResolveAsync(userId)`
grants the **owner's** full visibility. A key created for one organization can read and write every
organization the owner can see.

**Options under consideration** (no decision yet):
1. Don't give API-key principals role claims. Gap A endpoints then reject keys outright (or
   fail closed), and anything a key must reach moves to a permission policy.
2. Move `ImportExportController` and the `GridProfileService` check to permission policies
   (new `PermissionDefinitions` constants, seeded and granted to Owner/Admin) so scopes apply. Keep
   role claims for anything else that legitimately needs them.
3. For Gap B, add an explicit opt-in: either a permission per endpoint, or a marker
   (e.g. `[DenyApiKey]` / a policy that rejects principals carrying `ApiKeyId`), with
   interactive-only endpoints denied by default.
4. For Gap C, when `ApiKey.OrganizationId` is set, narrow the visible organizations to that
   organization (and its descendants?) intersected with the owner's visibility. Decide what a key
   with no `OrganizationId` should mean.

Pin down the expected behavior with tests (key created by an Owner with a single narrow scope →
403 on import/export, file delete, and other-org reads) before choosing.
