---
origin: DevCoreApp
targets: [ThreadIQ, Tentrie]
scope:
  - "*"   # affects namespace/folder layout of every layer in each fork
status: superseded
related:
  - docs/migration/out/2026-08-06-core-app-restructure.md   # ← apply this instead
  - CLAUDE.md ("Shared Core vs Product Code", "Cross-Project Sync")
  - docs/migration/README.md
---

> **Superseded — do not apply.** This doc proposed the convention before DevCoreApp had
> carried the move out itself. Two things changed in the doing: the marker rule
> ("directly above the feature") did not cover the projects grouped by kind, and a
> `Worker` layer listed here does not exist. Apply
> [`2026-08-06-core-app-restructure.md`](2026-08-06-core-app-restructure.md) instead — it
> states the finalized rules and the traps found while executing them.

# Adopt the Core / App structure and migration workflow

## Why

Fixes made to shared template code in one repo (DevCoreApp, ThreadIQ, or Tentrie) must be able to travel to the others. That is only tractable if every repo agrees on (a) how to tell *shared* code from *product-specific* code and (b) how to mark deliberate local overrides so they survive an upstream update. This doc asks each fork to adopt the same `Core`/`App` split, the same deviation-tag convention, and the same `docs/migration/{in,out}` folders that DevCoreApp now uses.

Do this once, and every future fix flows as an instruction doc through `docs/migration/` (see [`docs/migration/README.md`](../README.md)).

## Current state

- Namespaces are `DevInstance.{Product}.Server.<Layer>.<Feature>` (e.g. `DevInstance.Tentrie.Server.Admin.Services.Jobs`). Shared template code and product-specific code sit side by side in the same feature folders with nothing distinguishing them.
- No convention marks a local edit to template code, so upstream updates risk silently clobbering intentional product behavior.
- No `docs/migration/` folders exist.

## Proposed change

### 1. Core / App namespace + folder split (same assemblies, no new `.csproj`)

Introduce a marker segment **directly above the feature**:

- **Shared/template** code → folder `.../<Layer>/Core/<Feature>/`, namespace `...<Layer>.Core.<Feature>`.
- **Product-specific** code → folder `.../<Layer>/App/<Feature>/`, namespace `...<Layer>.App.<Feature>`.

Examples:

```
Shared:   DevInstance.Tentrie.Server.Admin.Services.Core.ApiKeys
Product:  DevInstance.Tentrie.Server.Admin.Services.App.Jobs
```

Apply the split in these layers: `Shared`, `Database/Core`, `Admin.Services`, `Admin.WebService`, `Worker`, `Email/*`, `Storage/*`, `Client`, `Client.Services`.

**Database exception.** The provider-agnostic `Database/Core` project already *is* the shared root — do **not** add a second `Core` (no `Core.Core`). Shared code keeps its current namespace (`Server.Database.Core.Models.ApiKey`); product entities move under an `App` segment (`Server.Database.Core.App.Models.<Entity>`). The `Postgres`/`SqlServer` provider projects are untouched by this split.

**Sync key.** A file's cross-repo identity = its namespace with the `DevInstance.{Product}` prefix stripped. Files with the same sync key and **no `.App.` segment** are the same shared surface and must stay in lockstep across all repos. Anything containing `.App.` is local and never synced.

### 2. Mark local deviations inside shared code

When you must edit a shared (`Core`) file in place rather than pushing the change into an `App/` file, fence it:

```csharp
#region project-specific Tentrie: <reason>
... deviating code ...
#endregion
```

`project-specific` is the greppable keyword. On an inbound update the fenced content is preserved; it is never sent back upstream. For non-C# shared files use the same keyword in that language's comment syntax with a matching close:

- Razor: `@* project-specific Tentrie: reason *@` … `@* /project-specific *@`
- JSON: `// project-specific …` … `// /project-specific`
- SQL: `-- project-specific …` … `-- /project-specific`
- XML / `.csproj`: `<!-- project-specific … -->` … `<!-- /project-specific -->`

### 3. Add the migration folders

Create `docs/migration/{in,out}` (and `in/applied/`), and copy DevCoreApp's [`docs/migration/README.md`](../README.md) and [`docs/migration/_template.md`](../_template.md) verbatim. From now on, contribute fixes upstream by authoring a doc in your `out/` and delivering it into DevCoreApp's `in/`.

## Affected shared surface

Every layer's namespace/folder layout changes. This is a large mechanical move; do it as its own reviewable commit per fork, verifying the build after each layer. Nothing about runtime behavior changes — only namespaces, `using`s, and file locations.

## Verification

- `dotnet build` succeeds after the move (all `using`s updated).
- `dotnet test` is green — no behavior changed.
- `rg "\.App\."` and `rg "\bCore\b" --type cs` reflect the intended split; no file sits in both.
- `rg "project-specific"` finds only real, closed fences (no dangling `#region`/`#endregion`).
- `docs/migration/README.md` and `_template.md` exist and match DevCoreApp's.

## Open questions

- Order of operations per fork: recommended sequence is Shared → Database/Core → Admin.Services → Admin.WebService → Worker → Email/Storage → Client, building between each.
- Whether to script the namespace rewrite (Roslyn/`sed`) or do it via IDE "move to namespace" refactors — either is fine; the end state is what matters.
