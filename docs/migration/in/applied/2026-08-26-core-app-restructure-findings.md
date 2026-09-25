---
origin: ThreadIQ
targets: [DevCoreApp]
scope:
  - Shared.Utils.Core.PhoneExtensions
  - Shared.Model.Core.Common.UrlExtensions
  - Shared.Model.Core.Common.OptionalUrlAttribute
  - Server.Admin.Services.Core.Files.ImageResizer
  - Server.Admin.Services.Core.Authentication.CurrentUserContext
  - Server.Admin.Services.Core.Authentication.ICurrentUserContext
  - Client.App                      # the App marker vs. the root App.razor component
  - Server.Admin.WebService.App     # same collision, milder
status: applied
related:
  - docs/migration/out/2026-08-06-core-app-restructure.md   # the doc these findings respond to
  - ThreadIQ docs/core-app-restructure-plan.md              # the file-by-file triage (ThreadIQ-local)
---

# Core/App restructure: two rule defects, and five helpers worth taking upstream

## Delivery & audit log — DevCoreApp

| Date | Event |
|---|---|
| 2026-08-26 | Authored in ThreadIQ `out/` after completing the restructure (`#1111`). Not yet delivered. |
| 2026-08-27 | Delivered into DevCoreApp `in/`. Not yet triaged. Findings 1 and 2 are rule defects DevCoreApp cannot reproduce on itself; finding 1 blocks any WASM fork with product code. |
| 2026-08-28 | Triaged and applied. Finding 1 reproduced here (CS0101 in Client, CS0118 in WebService) and fixed via option 1 — both Blazor shells now live in `UI/`. Findings 2-5 folded into `out/2026-08-06-core-app-restructure.md`; trap 5 corrected. All five helpers taken into canonical `Core`, two with changes. Organizations reconciliation: ThreadIQ to send it as its own instruction doc. Fan-out: `out/2026-08-28-app-marker-collision-and-core-helpers.md`. |

## Why

ThreadIQ carried out `2026-08-06-core-app-restructure.md` in full. The rules held, and the five
traps all fired as described. But ThreadIQ is the first fork to apply them to a repo where the
`App/` side is genuinely populated — 62 % of files — and that surfaced **two failure modes the doc
cannot warn about, because DevCoreApp is 100 % shared and its `App/` folders hold only a
`.gitkeep`.** One of them is a hard compile error with no workaround inside the current rules.

Separately, the triage found five files in ThreadIQ's `Core/` that have no upstream counterpart but
are plainly template surface, not CRM. They are offered here rather than being buried in `App/`.

## Finding 1 — `App` as a marker collides with `App.razor` (blocking)

**A Blazor project's root component is a type named `App`. The marker is a namespace named `App`.
When both sit in the same parent namespace, that is CS0101 — a namespace and a type of the same
name — and the project does not compile.**

It bites in two places, with different severity:

| | Root component's type | Marker namespace | Result |
|---|---|---|---|
| `Admin.WebService` | `…WebService.UI.App` (it lives in `UI/`) | `…WebService.App` | Compiles, but `MapRazorComponents<App>()` silently binds to the *namespace* and fails as `CS0118: 'App' is a namespace but is used like a type` |
| `Client` (WASM) | `…Client.App` (it lives at the project root, per Rule 2) | `…Client.App` | **CS0101. Hard stop.** |

DevCoreApp never sees either, because `…Client.App` and `…WebService.App` never come into
existence — nothing is under those folders.

What ThreadIQ did, for now:

- `Admin.WebService`: `app.MapRazorComponents<UI.App>()`.
- `Client`: added `@namespace DevInstance.ThreadIQ.Client.UI` to `App.razor`, fenced in place with
  `project-specific`, and `builder.RootComponents.Add<UI.App>("#app")`. The file keeps its path, so
  Rule 2's "fence it rather than move it" is honoured, but its **sync key changes** — which is
  exactly what Rule 5 is supposed to prevent.

Neither is satisfying. **This needs a rule decision, not a per-fork workaround.** Options, roughly
in order of how much we like them:

1. **Move the WASM root shell into `UI/`**, matching what `Admin.WebService` already does
   (`UI/App.razor`, `UI/Routes.razor`). One canonical layout for both hosts, the collision cannot
   recur, and the change lands in DevCoreApp so every fork inherits it. Costs one file move upstream.
2. **Name the marker something that cannot collide with a component** — `Product/`, `Local/`. Cheap
   for DevCoreApp (nothing to move), expensive for the two forks that have already applied the
   restructure.
3. Leave it, and add the workaround to the traps list. Every fork rediscovers it, and the WASM one
   rediscovers it as a build failure with a non-obvious cause.

We would take option 1.

## Finding 2 — scoped `.razor.css` files do not travel with their component

Trap-adjacent and easy to add. `Foo.razor.css` has to sit next to `Foo.razor`; a move list built
from `.cs`/`.razor` alone leaves it behind, and the build then fails with

```
error BLAZOR102: The scoped css file 'Core/UI/Components/Foo.razor.css' was defined but no
associated razor component or view was found for it.
```

It is caught at build time, so it is not dangerous — but the recipe in the doc says "`git mv` each
feature folder", which is what produced the orphan when individual components were pulled out of a
shared folder. Worth one line in the execution recipe: **when you move a component out of a folder,
move its `.razor.css` with it.**

ThreadIQ hit this on four components (`AttachmentTiles`, `AttachmentsSection`, `CompanyLogoUpload`,
`MarkdownSection`).

## Finding 3 — the rewrite has to handle partial qualifications, not just full ones

The recipe says to rewrite the concrete old→new namespace pairs. In practice C# code inside
`DevInstance.{Product}.*` writes those namespaces at **four** different lengths, all of which resolve
relatively and none of which a fully-qualified-only rewrite catches:

```csharp
DevInstance.ThreadIQ.Server.Database.Core.Models.CRM.DealRecord   // full
       Server.Database.Core.Models.CRM.DealRecord                 // product prefix dropped
              Database.Core.Models.CRM.DealRecord                 // …and more
                            Models.CRM.DealRecord                 // bare, from inside Database.Core
IOfflineCrudClientService<Shared.Model.CRM.DealItem>              // the same, in Client.Services
```

Generate the shorter forms from each pair and put them in the same alternation, longest-first. The
lookbehind that keeps a tail form from matching inside its own full form is
`(?<![A-Za-z0-9_.])`.

## Finding 4 — Rule 2 root files get swallowed by the exact-root mapping

When a project moves loose root files into `Core/`, the pair `<RootNs>` → `<RootNs>.Core` is
created — and it then matches the namespace declaration of the files Rule 2 says must **stay** at the
root, and of any sibling project sharing the prefix. ThreadIQ hit this three times:

- `BaseService.cs` / `ICRUDService.cs` were rewritten to `…Admin.Services.Core`
- `…Admin.Services.Mocks` would have become `…Admin.Services.Core.Mocks`
- `…EmailProcessor.MailKit` became `…EmailProcessor.Core.MailKit`, taking `ConfigurationExtensions.cs`
  with it

The fix is to add identity pairs (`X` → `X`) for the Rule 2 files' namespace and for sibling project
namespaces, so longest-first alternation matches them first and replaces them with themselves.

## Finding 5 — the entity move needs no migration (good news)

Trap 5 says to move product entities into `Database/Core/App/Models/` "as a separate change with a
migration". **Measured in ThreadIQ: it needs neither.** The 35 CRM/AI/Integrations entities plus
`Campaign.cs` moved with all 52 migration files and the 5,409-line `ModelSnapshot` left byte-for-byte
untouched, and `dotnet ef migrations has-pending-model-changes` reports none, before and after.

`MigrationsModelDiffer` matches entity types **by mapped table name first**; no table name moves, so
the differ emits zero operations. The stale type-name strings in the snapshot and the historical
`*.Designer.cs` build a name-keyed relational model that never resolves them to CLR types, so they
cannot throw, and the snapshot repairs itself on whatever migration lands next.

This is the same mechanic already recorded in ThreadIQ's `CLAUDE.md` for `RenameLeadToDeal`, read the
other way: there it produced `DropTable`+`CreateTable` precisely *because* table **and** type moved
together and neither matcher fired.

Suggested edit to trap 5: keep the warning, but say the move is free **as long as no table name
changes with it**, and name `dotnet ef migrations has-pending-model-changes` as the gate — it is
read-only, needs no database, and scaffolds nothing.

## Proposed change — five helpers for canonical `Core`

All five are in ThreadIQ's `Core/` today. None is CRM-specific; each solves a problem any fork has.

| File | What it is |
|---|---|
| `Shared.Utils.Core.PhoneExtensions` | `NormalizePhone()` / `FormatPhone()` / `PhoneDigits()` — the phone equivalent of the existing date/time rule: storage canonical E.164, UI formatted, decorator converts. Total functions; anything not confidently interpretable comes back unchanged rather than mangled. Deliberately **not** named `Normalize`, because `string.Normalize()` (Unicode) would win over an extension method of that name. |
| `Shared.Model.Core.Common.UrlExtensions` | `NormalizeUrlOrNull()` — user types a bare host, storage always keeps the scheme. Called on both sides of the decorator so legacy rows still render as absolute links. |
| `Shared.Model.Core.Common.OptionalUrlAttribute` | The validation attribute that goes with it. `[Url]` rejects a scheme-less host *and* flags a cleared field, which makes it wrong for every optional user-entered URL. |
| `Server.Admin.Services.Core.Files.ImageResizer` | Server-side image downscaling for uploads. Pairs with the existing `FileService`/`IFileStorageProvider` surface. |
| `Server.Admin.Services.Core.Authentication.{I,}CurrentUserContext` | `UserContext.NowLocal` — the Blazor-SSR-safe "now". Under SSR `DateTime.Now` is the *server's* clock, so seeding a `datetime-local` picker with it shows the wrong time to anyone outside the server's zone. This is the injectable form of the `NowInZone` helper canonical `Core` already has. |

They live in `Core/` in ThreadIQ on the assumption these are wanted upstream. If DevCoreApp declines
any of them, say so and ThreadIQ will move that one to `App/` — a file wrongly in `Core/` is the
dangerous direction, since the next fan-out treats it as shared surface.

## Verification

In ThreadIQ, after the full restructure:

```bash
dotnet build ThreadIQ.slnx                      # 0 errors
dotnet test  ThreadIQ.slnx                      # 463 passed, 0 failed
dotnet ef migrations has-pending-model-changes  # none
grep -rn "Core\.Core" --include='*.cs' src tests            # empty
git diff <base> -- '**/*.csproj' | grep -E "RootNamespace|AssemblyName"   # empty
git status --short -- src/Server/Database/Postgres/Migrations             # empty
```

## Open questions

- Finding 1 needs a decision before any further fork applies the restructure. A WASM fork with
  product code cannot complete it without one.
- ThreadIQ's `Pages/Admin/Organizations/{Index,Detail}` and canonical `Core`'s
  `OrganizationTreePage` are the same shared surface in different shapes. ThreadIQ has fenced its
  version in `Core/` and deferred reconciliation; that still needs a decision from the hub.
