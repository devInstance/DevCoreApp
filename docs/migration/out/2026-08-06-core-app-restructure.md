---
origin: DevCoreApp
targets: [ThreadIQ, Tentrie]
scope:
  - "*"   # every layer's namespace and folder layout
status: pending
related:
  - docs/migration/out/2026-07-23-adopt-core-app-structure.md   # superseded by this doc
  - docs/migration/in/applied/2026-08-26-core-app-restructure-findings.md   # ThreadIQ's execution report, folded in below
  - CLAUDE.md ("Shared Core vs Product Code", "Cross-Project Sync")
  - docs/migration/README.md
---

# Restructure into the Core / App split

## Amendments

**2026-08-28.** Revised after ThreadIQ executed this doc end to end and reported back
(`in/applied/2026-08-26-core-app-restructure-findings.md`). If you applied an earlier copy, the
deltas are: Rule 2 now puts the WASM root shell in `UI/` (trap 6 — a hard compile error, verified
in DevCoreApp), recipe steps 1 and 2 gained the `.razor.css`, partial-qualification and
identity-pair rules, and **trap 5 is corrected** — moving product entities needs no migration.

## Why

A fix made to shared template code in one repo must be able to travel to the others. That is only
tractable if every repo agrees on how to tell *shared* code from *product-specific* code, and how
to mark a deliberate local override so an upstream update does not silently clobber it.

DevCoreApp has now carried this out on itself (branch `core-app-restructure`, ~470 files). This doc
is the finalized rule set plus the traps found while executing it, so each fork can do the same
move without rediscovering them.

## Current state (in your repo)

- Namespaces are `DevInstance.{Product}.<Layer>.<Feature>`. Shared template code and product code
  sit side by side in the same feature folders with nothing distinguishing them.
- No convention marks a local edit to template code, so an upstream update risks overwriting
  intentional product behavior.
- `docs/migration/{in,out}` may not exist yet.

## Proposed change

### The six rules

**Rule 1 — the marker is the first segment under the project root namespace.**

```
<ProjectRootNamespace>.Core.<rest>     shared / template code
<ProjectRootNamespace>.App.<feature>   product-specific code
```

Folders mirror namespaces exactly: `<ProjectDir>/Core/<rest>`. Sync keys (product prefix stripped):

| File | Sync key |
|---|---|
| `Admin/Services/Core/ApiKeys/ApiKeyAdminService.cs` | `Server.Admin.Services.Core.ApiKeys` |
| `Admin/WebService/Core/Controllers/FileController.cs` | `Server.Admin.WebService.Core.Controllers` |
| `Admin/WebService/Core/UI/Pages/Admin/Users.razor` | `Server.Admin.WebService.Core.UI.Pages.Admin` |
| `Shared/Model/Core/ApiKeys/ApiKeyItem.cs` | `Shared.Model.Core.ApiKeys` |
| `Admin/Services/App/Jobs/JobService.cs` | *(local — never synced)* |

This is **namespaces + folders inside the existing assemblies**. No new `.csproj`; the dependency
graph does not change. `<RootNamespace>` / `<AssemblyName>` never gain a `Core` segment.

This is the rule that changed from the superseded doc. "Directly above the feature" works for
feature-grouped projects (`Admin.Services`, `Shared/Model`) but not for the kind-grouped ones
(`Admin.WebService` has `Controllers/`, `UI/Pages/`, `Health/`, `Hubs/` — there is no single
"feature" level). One marker directly under the project root covers both shapes.

**Rule 2 — files sitting *directly* at a project root keep the root namespace and get no marker.**
These are the host/entry/shell files the framework or convention pins in place. In DevCoreApp:

```
Admin.WebService/   Program.cs, _Imports.razor, UI/App.razor, UI/Routes.razor,
                    appsettings*.json, wwwroot/, Styles/, Scripts/
Admin.Services/     BaseService.cs, ICRUDService.cs
Client/             Program.cs, UI/App.razor, _Imports.razor, wwwroot/
Email/*, Storage/*  ConfigurationExtensions.cs   (DI wiring)
```

They are still shared surface — a fork that must change one fences it (Rule 6) rather than moving
it.

**Both Blazor root shells live in `UI/`, not at the project root.** A root component is a type
named `App` and the marker is a namespace named `App`; in the same parent namespace that is a
compile error. See trap 6.

**Rule 3 — the `Database/Core` project is exempt.** It already *is* the shared root; do not double
it to `Core.Core`. Shared code keeps `Server.Database.Core.<...>` unchanged; product entities go to
`Server.Database.Core.App.Models.<Entity>`. `Database/Postgres` and `Database/SqlServer` are
untouched.

**Rule 4 — the operative sync rule is `.App.`, not `Core`.** A file is local if and only if its
namespace contains an `.App.` segment; everything else is shared surface. This is what lets Rules 2
and 3 omit the marker without breaking anything — `Core` is a locator, not the source of truth.

**Rule 5 — sync key** = the namespace with the `DevInstance.{Product}` prefix stripped. Files with
the same sync key and no `.App.` segment are the same shared surface across all three repos.

**Rule 6 — fence local deviations inside shared code.** When a fork must edit a shared file in
place rather than pushing the change into an `App/` file:

```csharp
#region project-specific Tentrie: <reason>
... deviating code ...
#endregion
```

`project-specific` is the greppable keyword. Non-C# shared files use the same keyword with a
matching close in their comment syntax:

- Razor: `@* project-specific Tentrie: reason *@` … `@* /project-specific *@`
- JSON: `// project-specific …` … `// /project-specific`
- SQL: `-- project-specific …` … `-- /project-specific`
- XML / `.csproj`: `<!-- project-specific … -->` … `<!-- /project-specific -->`

On an inbound update the fenced content is preserved and never sent back upstream.

### Target layout (DevCoreApp's end state — mirror it for the shared parts)

```
Shared/Model/            Core/<Feature>/   + root DTOs under Core/
Shared/Utils/            Core/

Database/Core/           UNCHANGED  + App/Models/          (Rule 3)
Database/Postgres/       UNCHANGED
Database/SqlServer/      UNCHANGED

Admin/Services/          Core/<Feature>/  (ApiKeys, Appearance, AuditLogs, Authentication,
                             Background, BackgroundTasks, Email, Exceptions, FeatureFlags,
                             Files, ImportExport, Notifications, Organizations, Roles,
                             Seeding, Settings, UserAdmin, Webhooks)
                         Core/AccountService.cs, Core/GridProfileService.cs
                         BaseService.cs, ICRUDService.cs        (root, Rule 2)

Admin/WebService/        Core/{Controllers,Health,Hubs,Identity,Logging,Middleware}/
                         Core/UI/{Components,Layout,Model,Pages}/
                         UI/{App.razor,Routes.razor}            (root shell, Rule 2)
                         Program.cs, _Imports.razor, appsettings*.json,
                         wwwroot/, Styles/, Scripts/            (root)

Email/Processor/         Core/  + ConfigurationExtensions.cs at root
Email/{MailKit,SendGrid,Smtp}/    same shape
Storage/Processor/, {Local,S3}/   same shape

Client.Services/         Core/{Net,Net/Api,Notifications}/ + Core/ root services
Client/                  Core/{UI,Extensions}/
                         UI/App.razor                           (root shell, Rule 2)
                         Program.cs, _Imports.razor, wwwroot/   (root)

mocks/…/ServicesMocks/   Core/<Feature>/
tests/…/WebService/      Core/Services/       (mirrors Admin.Services)
tests/…/Database/Core/   UNCHANGED            (mirrors Database/Core, Rule 3)
tests/Shared/TestUtils/  Core/
```

Add an empty `App/` folder to each project that can host product code, so the destination is
visible.

### Step 0 (forks only) — triage before you move

DevCoreApp is 100% shared, so its move was purely mechanical. **In a fork this triage is the real
work.** For each file ask: *does a file with this sync key exist in DevCoreApp?*

- **Yes** → it belongs in `Core/`. Diff it against DevCoreApp's copy. Any delta is either
  (a) an upstream-worthy fix — author a doc in your `out/` and deliver it to DevCoreApp's `in/`, or
  (b) a deliberate product deviation — wrap it in a `project-specific` fence (Rule 6).
- **No** → it is product code; it belongs in `App/`.

Do the triage *before* moving files. A file that lands in `Core/` by mistake will be treated as
shared surface and overwritten by the next upstream fan-out.

### Execution recipe

Order, building after each: `Shared/Model` → `Shared/Utils` → `Email/*` → `Storage/*` →
`Admin.Services` → mocks → `Admin.WebService` → `Client.Services` → `Client` → tests.

For each project:

1. `git mv` each feature folder (and non-root loose files) into `<ProjectDir>/Core/`. **A
   `Foo.razor.css` must stay next to its `Foo.razor`** — when you pull individual components out of
   a shared folder rather than moving the folder whole, the scoped stylesheet is easy to strand.
   The build catches it (`BLAZOR102: The scoped css file … was defined but no associated razor
   component or view was found for it`), so it costs a round trip, not a bug.
2. Rewrite namespace tokens across **every `.cs` and `.razor` file in the whole solution** —
   consumers in other projects reference these namespaces too. Enumerate the concrete old→new
   pairs from the project's declared namespaces and apply them longest-first in a single pass, so
   `…Shared.Model` can never match as a prefix of `…Shared.Model.Account`. Match on namespace-token
   boundaries: not preceded by an identifier char or `.`, not followed by an identifier char (a
   trailing `.` is fine — that is the type name); `(?<![A-Za-z0-9_.])` is the lookbehind that keeps
   a short form from matching inside a longer one.

   Two things a fully-qualified-only rewrite misses, both found by ThreadIQ:

   - **Partial qualifications.** Code inside `DevInstance.{Product}.*` writes a namespace at every
     length that still resolves relatively, not just the full one:

     ```csharp
     DevInstance.ThreadIQ.Server.Database.Core.Models.CRM.DealRecord   // full
            Server.Database.Core.Models.CRM.DealRecord                 // product prefix dropped
                   Database.Core.Models.CRM.DealRecord                 // …and more
                                 Models.CRM.DealRecord                 // bare, from inside Database.Core
     IOfflineCrudClientService<Shared.Model.CRM.DealItem>              // the same, in Client.Services
     ```

     Generate the shorter forms from each pair and put them in the same longest-first alternation.

   - **Identity pairs for what must not move.** Folding loose root files into `Core/` creates the
     pair `<RootNs>` → `<RootNs>.Core`, which then matches the namespace declaration of the Rule 2
     files that must *stay* at the root, and of any sibling project sharing the prefix. ThreadIQ
     saw `BaseService.cs`/`ICRUDService.cs` rewritten to `…Admin.Services.Core`,
     `…Admin.Services.Mocks` heading for `…Admin.Services.Core.Mocks`, and
     `…EmailProcessor.MailKit` become `…EmailProcessor.Core.MailKit`. Add explicit `X` → `X` pairs
     for those namespaces so longest-first matches them first and replaces them with themselves.
3. Also rewrite `@using` lines and the **fully-qualified generic arguments inline in Razor markup** —
   e.g. `<HDataGrid TItem="DevInstance.{Product}.Shared.Model.ApiKeys.ApiKeyItem">`. These are easy
   to miss because they are in markup, not in a `using` block.
4. Build, then commit that project.

Preserve each file's BOM and line endings when rewriting, or the diff drowns in whitespace churn.

### The six traps (1-5 hit during DevCoreApp's move; 6 found by ThreadIQ and since verified here)

1. **`_Imports.razor` cascade.** A `_Imports.razor` applies to its own folder and below. Once the
   components move to `Core/UI/**`, a `_Imports.razor` still sitting in `UI/` no longer reaches
   them. **Move it to the project root** — from there it covers `UI/`, `Core/UI/**`, and any future
   `App/UI/**`. Nested ones (e.g. `Pages/Account/_Imports.razor`, which carries
   `[ExcludeFromInteractiveRouting]`) travel with their folder and need no change.

2. **`using` does not import nested namespaces.** `App.razor` / `Routes.razor` refer to components
   relatively — `typeof(Pages.NotFound)`, `typeof(Layout.MainLayout)`, `typeof(UI.Pages.NotFound)`.
   Adding `@using ….Core.UI` does **not** make `Pages.NotFound` resolve; a using-namespace
   directive imports types, not nested namespaces. Fully qualify instead:
   `typeof(Core.UI.Pages.NotFound)`, `typeof(Core.UI.Layout.MainLayout)`.

3. **Never rewrite `<RootNamespace>` / `<AssemblyName>`.** Restrict the rewrite to `.cs`/`.razor`
   and leave the root namespace itself unmapped (only *sub*-namespaces gain `.Core`). That single
   choice protects the two places the root string appears as data and would silently break:
   - `App.razor` → `Assets["/DevInstance.{Product}.Server.Admin.WebService.styles.css"]`
     (the scoped-CSS bundle name is the **assembly** name)
   - `appsettings.json` → `Database=aspnet-DevInstance.{Product}.Server-…`

4. **Namespace nesting stops resolving across the marker.** Types that used to resolve implicitly
   because a child namespace nested inside the parent (e.g. `EmailProcessor.MailKit` seeing
   `IEmailProvider` from `EmailProcessor`) stop resolving once the parent's types move to
   `EmailProcessor.Core`. Add explicit `using` directives. This also applies to any file staying at
   a project root under Rule 2 whose collaborators moved into `Core/`.

5. **Leave `Database/Core` and the provider projects alone** (Rule 3). This is what keeps EF
   migration snapshots valid: every type string in `Migrations/**` names
   `Server.Database.Core.Models.*`, all of which survive unchanged. **No migration needs
   regenerating.**

   Moving your own product entities into `App/Models/` is also free, and an earlier version of
   this doc was wrong to say otherwise. `MigrationsModelDiffer` pairs entity types by **mapped
   table name** first; a namespace-only move changes no table name, so the differ emits zero
   operations. The stale type strings left in the snapshot and in the historical `*.Designer.cs`
   build a name-keyed relational model that never resolves them to CLR types, so they cannot
   throw, and the snapshot repairs itself on whatever migration lands next. ThreadIQ measured it:
   36 entities moved, 52 migration files and a 5,409-line `ModelSnapshot` byte-for-byte untouched.

   Two conditions on that:

   - It holds only while **no table name moves with the type**. Renaming the class as well changes
     the default TPH discriminator (the short type name), which is a data change and does need a
     migration.
   - Gate it with `dotnet ef migrations has-pending-model-changes` — read-only, needs no database,
     scaffolds nothing. Run it **once per provider**; Postgres and SqlServer carry separate
     snapshots.

6. **`App` as a marker collides with the Blazor root component.** A Blazor host's root component
   is a type named `App`; the marker is a namespace named `App` under the same project root. The
   moment anything lands under `App/`, that is a compile error — and it will not show up while
   your `App/` folders are still empty:

   | Host | Root component | Result |
   |---|---|---|
   | `Client` (WASM), shell at project root | `…Client.App` | `CS0101: the namespace already contains a definition for 'App'` — hard stop |
   | `Admin.WebService`, shell already in `UI/` | `…WebService.UI.App` | compiles, then `CS0118: 'App' is a namespace but is used like a type` at `MapRazorComponents<App>()` |

   Fix, and the reason Rule 2 now lists `UI/App.razor` for both hosts: `git mv App.razor
   UI/App.razor` in the WASM client, and reference the shell qualified in both hosts —
   `builder.RootComponents.Add<UI.App>("#app")` and `app.MapRazorComponents<UI.App>()`. Do not
   work around it with an `@namespace` directive on `App.razor`: the file keeps its path but its
   sync key changes, which is the one thing Rule 5 exists to prevent.

   To prove it is fixed, drop a throwaway `namespace <RootNs>.App; internal class Probe { }` into
   `Client/App/` and `Admin.WebService/App/`, build the solution, and delete it.

### Add the migration folders

Create `docs/migration/{in,out}` (and `in/applied/`), and copy DevCoreApp's
[`README.md`](../README.md) and [`_template.md`](../_template.md) verbatim.

## Affected shared surface

Every layer's namespace and folder layout. Nothing about runtime behavior changes — only
namespaces, `using`s, and file locations. Do it as its own reviewable commit per project.

Because this touches every shared file, land it **before** applying any other pending inbound doc;
otherwise every later doc's paths need translating.

## Verification

```bash
dotnet build <Solution>            # 0 errors
dotnet build -c ServiceMocks <Solution>
dotnet test  <Solution>            # same test count as before the move
```

Structural checks:

```bash
# every namespace is root-only, Core-marked, App-marked, or an allowed Database exception
grep -rhoE '^namespace +[A-Za-z0-9_.]+' --include='*.cs' src mocks tests | sort -u

grep -rn "Core\.Core" --include='*.cs' src tests      # → empty
git diff --stat <base> -- src/Server/Database          # → only App/ placeholders
git diff <base> -- '**/*.csproj' | grep -E "RootNamespace|AssemblyName"   # → empty

# fences well-formed: every #region project-specific has a matching #endregion
grep -rn "project-specific" --include='*.cs' --include='*.razor' src
```

Runtime smoke test — this is what catches Razor/DI breakage that compiles fine:

```bash
dotnet run -c ServiceMocks --project <WebService.csproj>
```

Confirm `/` renders (proves the `_Imports.razor` cascade and `MapRazorComponents<App>()` still
resolve), an admin grid page loads (proves component resolution and the inline `TItem` rewrites),
`/health` returns healthy, and the login/account pages render (proves the nested
`Core/UI/Pages/Account/_Imports.razor` survived).

## Open questions

- **Test-project namespaces.** DevCoreApp left two pre-existing inconsistencies alone rather than
  bundling unrelated renames into a 470-file move: `tests/Server/Database/Core/Core.Tests.csproj`
  has root namespace `Core.Tests` with no product prefix, and one test file under
  `tests/.../WebService/Core/Services/` declares `Server.Services.Tests` instead of
  `Server.Tests.Services`. Fix them in your repo if you prefer; they are not shared surface.
- **How far to take `App/`.** DevCoreApp added `App/` placeholders only to projects that
  realistically host product code, not to the small `Email/*` / `Storage/*` provider projects.
  A fork with more product surface may want them everywhere.
