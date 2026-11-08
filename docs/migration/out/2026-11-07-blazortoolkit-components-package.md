---
origin: DevCoreApp
targets: [ThreadIQ, Tentrie]
scope:
  - all project files                                                     # BlazorToolkit 10.7.0, Microsoft.* 10.0.14, LogScope* 10.1.1, WebServiceToolkit.Common 10.4.0
  - Client.Desktop project file                                           # + DevInstance.BlazorToolkit.Components 10.7.0; RZ10012 as error
  - Client.Mobile project file                                            # RZ10012 as error
  - Client.Desktop.Core.UI.Components.HDataGrid / GridSettings / HColumnTemplate / HSortableHeader / HSortableHeaderSortArgs   # deleted → package
  - Client.Desktop.Core.UI.Components.HSearchBar / SearchHighlight        # deleted → package
  - Client.Desktop.Core.UI.Model.Grid.ColumnDescriptor / GroupContext     # deleted → package
  - Client.Desktop.Core.UI.Model.Grid.GridProfileColumnExtensions         # new in DevCoreApp (from ThreadIQ)
  - Client.Desktop.Core.UI.Pages.Admin.* (10 grid pages)                  # ApplyProfile / ToColumnStates; profile save outside Host
  - Client.Desktop _Imports.razor, wwwroot/index.html
  - Shared.Model.Core.Common.HighlightRange                               # deleted → WebServiceToolkit.Common.Model
  - Shared.Model.Core.Common.PagedList                                    # + IModelListHighlights
  - Shared.Model.Core.Settings.GridProfileItem (GridColumnState)          # + Width, IsGrouped
  - tests Client.Services.Tests Core.HighlightRangeTests                  # using only
  - Client.Desktop Core/UI/Components/HDataGrid.md, docs/Api.md, CLAUDE.md
status: pending
related:
  - https://github.com/devInstance/BlazorToolkit (commit 7fc3a90 — "Added HDataGrid to DevInstance.BlazorToolkit.Components (version 10.7.0), ported from ThreadIQ")
  - https://github.com/devInstance/BlazorToolkit/blob/master/docs/components.md
  - docs/migration/out/2026-09-26-wasm-client-and-api-layer.md        # Tentrie: prerequisite (see "Per-fork notes")
  - docs/migration/in/applied/2026-10-20-search-highlight-ranges.md   # the HighlightRange / SearchHighlight this replaces
---

# Use BlazorToolkit 10.7.0 and its separate Components package (HDataGrid moves to NuGet)

## Delivery & audit log

| Date | Event |
|---|---|
| 2026-11-07 | Applied in DevCoreApp and authored in `out/`. Not yet delivered. |

## Why

The data grid (`HDataGrid` and its companions), the search bar and the search-highlight renderer
were copied by hand into every repo's Desktop client, and the copies had already diverged:
ThreadIQ's grid gained grouping and column resizing that DevCoreApp's did not have. BlazorToolkit
now ships them:

- **BlazorToolkit 10.6.0** moved the UI components (`ErrorMessageBanner`, `PageProgress`,
  `LoadingProgress`, `DataPager`, `ModelDataPager`, `SortableHeader`, `BlazorToolkitPageLayout`)
  out of `DevInstance.BlazorToolkit` into the new **`DevInstance.BlazorToolkit.Components`** package.
  The namespace (`DevInstance.BlazorToolkit.Components`) did not change. `ServiceExecutionHostComponent`
  stays in the core package.
- **BlazorToolkit 10.7.0** added ThreadIQ's grid to that package: `HDataGrid`, `HColumnTemplate`,
  `GridSettings`, `ColumnDescriptor`, `GroupContext`, `HSortableHeader`
  (`…Components.DataGrid`) and `HSearchBar`, `SearchHighlight`, `HighlightsFor`
  (`…Components.Search`). It needs **WebServiceToolkit.Common 10.4.0**, which now owns
  `HighlightRange` and the new `IModelListHighlights` interface.

**Trap:** without the Components package, Razor does **not** fail the build. It emits warning
RZ10012 and the component silently renders nothing. That is why both client projects now treat
RZ10012 as an error.

The grid then lives in one place and its fixes ship as a package update. Do not copy it back into
`Core/`. Grid bugs are fixed in BlazorToolkit.

## Proposed change

### 1. Packages (every project file)

| Package | To | Note |
|---|---|---|
| `DevInstance.BlazorToolkit` | **10.7.0** | every project that references it |
| `DevInstance.BlazorToolkit.Components` | **10.7.0** (new) | Desktop only. Mobile keeps its own copies under `Core/UI/Components/Shared` and does not need it, unless the fork's Mobile uses a package component |
| `DevInstance.WebServiceToolkit.Common` | **10.4.0** | Shared.Model |
| all `Microsoft.*` (ASP.NET Core, EF Core, Extensions) | **10.0.14** | required: BlazorToolkit 10.7.0 depends on 10.0.14, and older direct references fail with NU1605 "package downgrade" |
| `DevInstance.LogScope`, `.NET`, `.Extensions.SerilogLogger` | **10.1.1** | same NU1605 reason |

In both client project files, add to the first `<PropertyGroup>`:

```xml
<!-- A missing component package only warns (RZ10012) and renders nothing; fail the build instead. -->
<WarningsAsErrors>$(WarningsAsErrors);RZ10012</WarningsAsErrors>
```

### 2. Delete the local copies (Desktop)

`Core/UI/Components/`: `HDataGrid.razor`, `HDataGrid.razor.cs`, `GridSettings.razor`,
`GridSettings.razor.cs`, `HColumnTemplate.razor`, `HSortableHeader.razor`,
`HSortableHeaderSortArgs.cs`, `HSearchBar.razor`, `HSearchBar.razor.cs`, `HSearchBar.razor.css`,
`SearchHighlight.cs`.
`Core/UI/Model/Grid/`: `ColumnDescriptor.cs`, `GroupContext.cs`.

Shared.Model: delete `Core/Common/HighlightRange.cs`. The WebServiceToolkit one is byte-for-byte
the same apart from the namespace (`DevInstance.WebServiceToolkit.Common.Model`), and the JSON
shape is unchanged. Make `PagedList<T>` opt in:

```csharp
public class PagedList<T> : IModelList<T>, IModelListHighlights
```

Keep `PagedList.HighlightsFor` / `SortField` / `IsAscending`. They are DevCoreApp's own, and
`HighlightsFor` on `PagedList<T>` wins overload resolution over the package's `IModelList<T>` one.

### 3. Wire the package in (Desktop)

`_Imports.razor`: replace `@using …Client.Desktop.Core.UI.Model.Grid` with

```razor
@using DevInstance.BlazorToolkit.Components.DataGrid
@using DevInstance.BlazorToolkit.Components.Search
```

and keep `@using DevInstance.BlazorToolkit.Components` (it already exists, for `ModelDataPager`).
In code-behinds, `using …Core.UI.Model.Grid;` becomes `using DevInstance.BlazorToolkit.Components.DataGrid;`.
Keep the old using as well where the file calls `GridProfileColumnExtensions` (step 4). Files that
use `HighlightRange` add `using DevInstance.WebServiceToolkit.Common.Model;`.

`wwwroot/index.html`: Bootstrap 5.3 CSS **and** the JS bundle plus Bootstrap Icons are required
(already present). Add the package stylesheet after Bootstrap and **before** `app.css`, so the app
can still override it:

```html
<link rel="stylesheet" href="_content/DevInstance.BlazorToolkit.Components/css/blazortoolkit-components.css" />
```

The grid imports its own ES module (`_content/DevInstance.BlazorToolkit.Components/js/blazortoolkit-components.js`).
**No `<script>` tag.**

### 4. Persist grouping and resized widths in grid profiles

The package grid lets the user group by a column (header menu, group bar, Grid Settings) and drag
column widths. Both are **on by default** (`EnableGrouping`, `ResizableColumns`) and are reported
through `OnColumnsChanged`. Unless they are saved, they reset on every reload.

`Shared.Model.Core.Settings.GridColumnState` gets ThreadIQ's two fields, **with ThreadIQ's names**.
ThreadIQ's stored profiles already use them, so keep the names exactly:

```csharp
/// <summary>Width the user dragged the column to (a percentage, e.g. "23.50%"); null keeps the page default.</summary>
public string? Width { get; set; }
/// <summary>True for the column the grid is grouped by (at most one).</summary>
public bool IsGrouped { get; set; }
```

`GridProfile.ColumnsJson` is JSON, so **no migration**. Old rows deserialize with the defaults.

New `Client.Desktop.Core.UI.Model.Grid.GridProfileColumnExtensions` (ThreadIQ's file, plus
`using DevInstance.BlazorToolkit.Components.DataGrid;`): `columns.ApplyProfile(profile)` applies
visibility, `ResizedWidth ← Width`, `IsGrouped` and the saved order; `columns.ToColumnStates()`
builds the list to save. In every grid page, `ApplyGridProfile` and `SaveGridProfile` shrink to:

```csharp
Columns = Columns.ApplyProfile(profile);          // ApplyGridProfile, after page size / sort
...
Columns = Columns.ToColumnStates()                // in the GridProfileItem initializer
```

EmailLog (page-defined `GroupBy` when "group by status" is on) hides the settings drawer's picker:
`<GridSettings … EnableGrouping="!GroupByStatus" />`. Do the same on any page that passes `GroupBy`.

### 5. Save the grid profile outside the execution host (bug fix)

Every grid page binds `IsLoading="Host.InProgress"`, and `SaveGridProfile` called
`Host.ServiceSubmitAsync(() => GridProfileService.SaveAsync(profileItem))`. That flips
`Host.InProgress`, so **every resize, column move, hide, group change and sort flashed the grid into
skeleton rows**, as if it were reloading. Resizing makes this very visible. ThreadIQ has the same
code. Call the service directly:

```csharp
// Not through Host: it would flip Host.InProgress, which the grid shows as loading rows on
// every resize / move / sort. A layout preference that fails to save is not worth an error banner.
await GridProfileService.SaveAsync(profileItem);
```

`ApiServiceBase.CallAsync` goes through BlazorToolkit's `ServiceUtils.HandleWebApiCallAsync`, which
catches every exception, logs it (`IScopeLog`) and returns a failed `ServiceActionResult`. So a
failed save never throws into the page. It is logged but not shown, which is deliberate for a
layout preference. Apply this to **App/** grid pages too
(ThreadIQ: CRM, AI and report index pages).

### 6. Docs

`Core/UI/Components/HDataGrid.md` is reduced to the app-specific parts: package pointer, grid
profile pattern (with steps 4–5) and the agent instructions ("do not modify or copy HDataGrid").
The component reference is BlazorToolkit's `docs/components.md`. `docs/Api.md` (highlights) and
the root `CLAUDE.md` tech-stack list name the package.

## Behaviour changes users will see

- Column grouping and drag-to-resize on every grid, both persisted per user.
- `HSearchBar` has a separate primary search button next to the input; the in-input icon no longer
  submits. The package took ThreadIQ's layout.
- The Grid Settings toggle icon changed (`bi-sliders`), the drawer was restyled and gained a
  "Group by" picker.
- Search highlights look the same, unless the app overrides `mark.search-mark` (DevCoreApp's
  `Styles/scss/_components.scss` still does, and wins because `app.css` loads later).

## Affected shared surface

See `scope` above. Fences to watch: any `#region project-specific` inside the deleted component
files is lost with the file. Move the behaviour upstream to BlazorToolkit, or to an `App/` wrapper,
**before** deleting.

## Per-fork notes

**ThreadIQ** (at `13310e5`: BlazorToolkit 10.5.2, WebServiceToolkit.Common 10.3.0, LogScope 10.1.1)

- The package grid *is* ThreadIQ's grid, ported in BlazorToolkit `7fc3a90`. Before deleting the
  local copies, diff them against `BlazorToolkit/src/Components/DataGrid` and `…/Search`. The
  differences seen at port time are expected: `IModelList<T>` instead of `PagedList<T>`, the JS
  loaded as an ES module instead of the global `hDataGrid` object, and `SettingsId` / `Id` /
  `ShowToggleButton` for several grids on one page. Anything ThreadIQ changed after `13310e5`
  goes upstream first.
- Also delete what only the local grid used: `Scripts/data-grid.ts`, `wwwroot/scripts/data-grid.js`
  (+ `.map`), its `<script src="scripts/data-grid.js">` in `index.html`, the `hDataGrid` declaration
  in `Scripts/globals.d.ts`, and the `.hdg-*` / `body.hdg-resizing` rules in
  `Styles/scss/_components.scss` (the package stylesheet has them; check for app-specific tweaks
  first).
- `GridColumnState.Width` / `IsGrouped` and `GridProfileColumnExtensions` already exist. Only add
  the `DataGrid` using to the extensions file. Pages already call `ApplyProfile` / `ToColumnStates`.
- Step 5 applies to all grid pages, Core and App.

**Tentrie** (at `c38da6d`: BlazorToolkit 10.1.2, LogScope 10.1.0, admin UI still Blazor Server in
`Server/Admin/WebService/UI`)

- Tentrie has not taken the WASM client doc (`2026-09-26-wasm-client-and-api-layer.md`) yet.
  Apply that first. The paths in this doc assume `Client.Desktop`, and it also carries the
  BlazorToolkit 10.4.0 `ServiceActionErrorType` renumbering that 10.7.0 includes.
- If this doc has to come first: the package works in Blazor Server. Do steps 1–3 against the
  Blazor Server UI project, and in step 3 use `@Assets["_content/…/blazortoolkit-components.css"]`
  in `App.razor`. Then step 4 needs both `GridColumnState` fields and the extensions file; Tentrie
  has neither.

## Verification

```bash
dotnet build <Solution>.slnx                      # 0 errors; no RZ10012 (it is an error now)
dotnet build -c ServiceMocks <Solution>.slnx      # mocks compile too
dotnet test <Solution>.slnx
grep -rn "Core.UI.Model.Grid.ColumnDescriptor\|class HDataGrid\|class HighlightRange" src   # nothing left
```

Then run the Desktop client (DevCoreApp: `dotnet run -c ServiceMocks --project src/Client/…Client.Desktop`):

- `_content/DevInstance.BlazorToolkit.Components/css/blazortoolkit-components.css` and
  `…/js/blazortoolkit-components.js` return 200.
- On Users: drag a column border. The width changes, **the grid does not switch to loading rows**,
  and the width survives a reload. Group by a column from the header menu, then reload: still
  grouped.
- Search on Users: matches are highlighted.
- Open Grid Settings from the header menu and from the edge button.
