---
origin: DevCoreApp
targets: [ThreadIQ, Tentrie]
scope:
  - Client.UI.App                                                # was Client.App — the shell moved
  - Client.Program
  - Server.Admin.WebService.Program
  - Shared.Utils.Core.PhoneExtensions                             # new
  - Shared.Utils.Core.DateTimeExtensions                          # gains ResolveTimeZone
  - Shared.Model.Core.Common.UrlExtensions                        # new
  - Shared.Model.Core.Common.OptionalUrlAttribute                 # new
  - Server.Admin.Services.Core.Files.ImageResizer                 # new, signature differs from ThreadIQ's
  - Server.Admin.Services.Core.Authentication.ICurrentUserContext # new
  - Server.Admin.Services.Core.Authentication.CurrentUserContext  # new, body differs from ThreadIQ's
  - Server.Admin.Services.Core.Authentication.ConfigurationExtensions
  - Server.Admin.Services.BaseService
  - Server.Admin.Services.Core.UserAdmin.UserProfileService
status: pending
related:
  - docs/migration/in/applied/2026-08-26-core-app-restructure-findings.md   # the inbound this answers
  - docs/migration/out/2026-08-06-core-app-restructure.md                   # amended, see "For Tentrie"
  - CLAUDE.md ("Shared Core vs Product Code" — Rules 2 and 6)
---

# Fix the `App` marker collision, and take five helpers into canonical `Core`

## Why

ThreadIQ applied `2026-08-06-core-app-restructure.md` in full and reported back. Its findings held
up: every one reproduced or checked out here, including a **hard compile error that DevCoreApp
could not see** because its own `App/` folders were empty placeholders. This doc carries the
resulting rule change, the corrected instructions, and the hub's decisions on the five helpers
ThreadIQ offered upstream.

## Current state

The marker namespace `App` collides with the Blazor root component type `App`. Verified here by
dropping one throwaway class into each empty `App/` placeholder and building:

| Project | What happens |
|---|---|
| `Client` (WASM), shell at project root | `App_razor.g.cs: CS0101: The namespace 'DevInstance.DevCoreApp.Client' already contains a definition for 'App'` |
| `Admin.WebService`, shell already in `UI/` | `Program.cs: CS0118: 'App' is a namespace but is used like a type` |

So the placeholders shipped by the restructure doc were booby-trapped: the first fork to put a file
under `Client/App/` cannot build, and the cause is several steps removed from the change that
triggered it.

ThreadIQ worked around it with an `@namespace` directive on `App.razor`, fenced in place. That
keeps the file's path but changes its **sync key** — precisely what Rule 5 exists to prevent — so
it needed a decision at the hub rather than a per-fork patch.

## Proposed change

### 1. Both Blazor root shells live in `UI/`

This is ThreadIQ's option 1, adopted. `Admin.WebService` already had `UI/App.razor`; the WASM
client now matches it. One canonical layout for both hosts, and the collision cannot recur.

```
git mv src/Client/<Product>.Client/App.razor src/Client/<Product>.Client/UI/App.razor
```

The Razor generator derives the namespace from the folder, so the component becomes
`DevInstance.{Product}.Client.UI.App` with no `@namespace` directive. Remove the fenced
`@namespace` line if you added one.

**Reference the shell qualified in both hosts.** A bare `App` written from a project root namespace
binds to the marker *namespace*, not the component:

```csharp
// Client/Program.cs
builder.RootComponents.Add<UI.App>("#app");

// Admin/WebService/Program.cs
app.MapRazorComponents<UI.App>()
```

Rule 2's root-file list changes accordingly — `Client/` is now `Program.cs, UI/App.razor,
_Imports.razor, wwwroot/`. `_Imports.razor` stays at the project root and still cascades to `UI/`.

**The general rule, now in CLAUDE.md:** no type may share its name with a marker namespace in the
same parent namespace. `App` is the only instance today, but it is worth stating once rather than
rediscovering per fork.

**How to check.** Neither DevCoreApp nor a fork with empty markers can catch a regression here by
building. Drop a throwaway file into each marker folder, build, delete it:

```bash
printf 'namespace DevInstance.%s.Client.App;\ninternal class Probe { }\n' "$PRODUCT" \
  > src/Client/*.Client/App/Probe.cs
# same for Admin/WebService/App/Probe.cs, then build, then delete both
```

### 2. Rule 6 marks a region, not a file

Clarification prompted by ThreadIQ's Organizations pages, which are fenced as whole files inside
`Core/`. A fence is for a local override *inside* a shared file. If a fork ends up wrapping an
entire shared file — or replacing one shared file with a differently shaped set of files — that is
product code in the wrong folder: move it to `App/`, and if the upstream original is genuinely
obsolete, send the replacement to the hub as its own instruction doc. A whole-file fence left in
`Core/` reads to the next fan-out as "shared surface, do not touch", which is the opposite of what
is meant.

### 3. Five helpers accepted into canonical `Core`

All five are in, with two changed on the way through. **ThreadIQ: re-sync your copies to these
shapes** — the sync keys are unchanged, so they must stay in lockstep.

| Sync key | Status |
|---|---|
| `Shared.Utils.Core.PhoneExtensions` | Taken verbatim. |
| `Shared.Model.Core.Common.UrlExtensions` | Taken verbatim, plus a comment recording *why* it sits in Shared.Model rather than beside `PhoneExtensions`: `Shared.Model.csproj` has no project references, so `OptionalUrlAttribute` could not reach `Shared.Utils`. |
| `Shared.Model.Core.Common.OptionalUrlAttribute` | Taken verbatim. |
| `Server.Admin.Services.Core.Files.ImageResizer` | **Changed — see below.** |
| `Server.Admin.Services.Core.Authentication.{I,}CurrentUserContext` | Interface verbatim; implementation **changed — see below**. |

**`ImageResizer` — the encode format is now a parameter.** The offered version emitted PNG only.
Canonical `UserProfileService` stores JPEG at quality 85 and writes a matching
`ProfilePictureContentType = "image/jpeg"`, so a PNG-only helper would have silently changed both
the stored bytes and the column's truthfulness. The general method is:

```csharp
public static byte[]? Resize(byte[] imageData, int maxWidth, int maxHeight,
                             SKEncodedImageFormat format, int quality = 90)
```

`ResizeToPng(...)` remains as a thin wrapper, so ThreadIQ's existing call sites keep working
unchanged. `UserProfileService.ResizeImage` is now a private wrapper that calls
`Resize(..., SKEncodedImageFormat.Jpeg, 85)` and turns a null (undecodable upload) back into the
`BadRequestException` its callers already expect.

Two behavioral improvements come along with the consolidation, both from ThreadIQ's version:
`SKSamplingOptions(SKCubicResampler.Mitchell)` replaces the obsolete `SKFilterQuality.High`, and
target dimensions are rounded with a floor of 1 instead of truncated — an extreme aspect ratio
previously truncated to a zero-length side. Verified against **SkiaSharp 3.\***; no package bump
needed.

**`CurrentUserContext` — timezone resolution has one home.** The offered version inlined its own
`FindSystemTimeZoneById` + catch, which is the third copy of that logic (canonical `BaseService`
already had one). The lenient resolver now lives in `Shared.Utils.Core.DateTimeExtensions`:

```csharp
public static TimeZoneInfo? ResolveTimeZone(string? timeZoneId)
```

`CurrentUserContext.TimeZone` and `BaseService.ResolveTimeZone` both delegate to it, so a page and
a service can no longer disagree about what a user's timezone is. `BaseService.ResolveTimeZone`
keeps its `protected static` signature — existing subclasses are unaffected.

Registration is one line in `Server.Admin.Services.Core.Authentication.ConfigurationExtensions`,
in the same position ThreadIQ used:

```csharp
services.AddScoped<ICurrentUserContext, CurrentUserContext>();
```

Note these six files carry a `// Copyright (c) DevInstance LLC.` header, which almost nothing else
in DevCoreApp does. That is deliberate: it keeps each file's cross-repo diff down to the single
namespace line.

### 4. For Tentrie — the restructure doc was amended, not superseded

Tentrie has not applied `2026-08-06-core-app-restructure.md` yet, so it should apply the **amended**
copy rather than this doc's section 1. The amendments (all from ThreadIQ's execution, all now in
that doc):

- **Trap 6** — the `App`/`App.razor` collision above, with the `UI/` layout baked into Rule 2.
- **Recipe step 1** — a `Foo.razor.css` must travel with its `Foo.razor`. Pulling components out of
  a shared folder one at a time strands the scoped stylesheet; the build fails with `BLAZOR102`.
- **Recipe step 2** — the namespace rewrite must also handle **partial qualifications** (code writes
  a namespace at every length that still resolves relatively, not just the fully-qualified form)
  and needs **identity pairs** (`X` → `X`) for the Rule 2 root files and sibling projects, or the
  `<RootNs>` → `<RootNs>.Core` pair swallows the very files that must stay put.
- **Trap 5 corrected** — moving product entities into `App/Models/` needs **no migration**.
  `MigrationsModelDiffer` pairs entity types by mapped table name, so a namespace-only move emits
  zero operations; the stale type strings in the snapshot are inert. Gate it with
  `dotnet ef migrations has-pending-model-changes`, run once per provider. The exception is a class
  **rename**, which changes the default TPH discriminator and does need a migration.

## Affected shared surface

Everything in `scope` above. Collision risk in a receiving fork:

- `Client.App` → `Client.UI.App` is a **sync key change**. Any fork that fenced an `@namespace`
  directive onto `App.razor` should drop the fence and move the file instead.
- `Server.Admin.Services.BaseService` is a Rule 2 root file — check for a local fence before
  editing `ResolveTimeZone`.
- `Server.Admin.Services.Core.UserAdmin.UserProfileService` is large and commonly deviated from;
  only the private `ResizeImage` method and one `using` change here.

## Verification

```bash
dotnet build <Solution>                 # 0 errors
dotnet build -c ServiceMocks <Solution>
dotnet test  <Solution>                 # same count as before

# the collision is actually gone (see section 1 for the probe files)
#   with a Probe.cs in Client/App/ and Admin/WebService/App/, the solution still builds

grep -rn "MapRazorComponents<App>\|RootComponents.Add<App>" src   # -> empty
grep -rn "@namespace" src/Client/*/UI/App.razor                   # -> empty
```

Runtime smoke test: `dotnet run -c ServiceMocks --project <WebService.csproj>`, confirm `/` renders
(the shell still resolves) and an admin grid page loads.

In DevCoreApp this landed with 0 errors, 372 warnings (down from 375 — the obsolete
`SKFilterQuality` call is gone), and 17/17 tests passing.

## Open questions

- **Organizations pages.** ThreadIQ's `Core/UI/Pages/Admin/Organizations/{Index,Detail}` replaces
  canonical `Core`'s single `OrganizationTreePage` and is currently whole-file fenced inside
  `Core/`. Per section 2 that is not a valid fence. The hub's answer: **send it as its own
  instruction doc** — an `out/` doc arguing the two-page shape on its merits, so the hub can adopt
  it as canonical (retiring `OrganizationTreePage` for everyone) or decline it, in which case the
  pair moves to `App/`. Until that doc exists, please leave the current fence in place rather than
  deleting either version.
- Nothing else from the inbound findings is outstanding.
