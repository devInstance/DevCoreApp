---
origin: ThreadIQ
targets: [DevCoreApp]
scope:
  - Shared.Model.Core.Common.PagedList            # new Highlights property + PagedList.HighlightsFor extension
  - Shared.Model.Core.Common.HighlightRange       # new: wire type + Segments() splitter
  - Server.Services.Core.Search.SearchHighlighter # new: SearchHighlighter + SearchHighlightBuilder<T>
  - Server.Services.ICRUDService                  # doc comment only (root file, no marker)
  - Client.Desktop.Core.UI.Components.SearchHighlight   # new component (plain .cs, no .razor)
  - Client.Desktop.Core.UI.Components.HDataGrid         # HDataGrid.razor + HDataGrid.razor.cs + HDataGrid.md
  - Client.Desktop.Core.UI.Model.Grid.ColumnDescriptor  # new HighlightField
  - Client.Desktop Styles/scss/_components.scss         # root asset, no marker: mark.search-mark rule
  - docs/Api.md                                         # canonical wire contract: "highlights" on PagedList
  - Server.Database.Core.Data.Queries.Core*Query        # 14 shared queries: Search() made case-insensitive (list below)
status: applied
change: "fa887ee"   # ThreadIQ dev/1269-search-highlight: "#1269: Search doesn't have highlight anymore". The doc inside that commit still has the placeholder; read this doc from main.
related:
  - ThreadIQ issue #1269 "Search doesn't have highlight anymore"
  - docs/Api.md (canonical, shared) — "Lists and queries"
---

# Search becomes case-insensitive, and highlighting returns as server-computed match ranges on `PagedList<T>`

## Delivery & audit log — DevCoreApp

| Date | Event |
|---|---|
| 2026-10-20 | Authored in ThreadIQ `out/` under `#1269`. Not yet delivered. |
| 2026-10-20 | `change` filled in (`fa887ee`). |
| 2026-11-07 | Applied in DevCoreApp. Deviations: `SearchHighlighter`/`SearchHighlightBuilder<T>` constrain `T : IModelItem` (not `ModelItem`) because DevCoreApp DTOs implement the interface; the test `Row` implements `IModelItem` accordingly. `CoreUserProfilesQuery` has no phone-digit match in DevCoreApp, so only the case change applied; `CoreOrganizationsQuery` lacks the Location/Contact* columns (ThreadIQ product fields). Query test added as `tests/Server/Database/Core/UserAdmin/UserProfilesQuerySearchTests.cs`. No list opted in yet (Users `useSearchMarkup` open question still stands). |

## Why

Two problems with the same search box, fixed together because the highlight has to match the way
search matches.

**1. Search is case-sensitive.** Every query's `Search()` compares with `string.Contains` /
`IndexOf(search) >= 0`, which PostgreSQL runs as a case-sensitive `LIKE`/`strpos`: "PLain" never finds
"Plain". That applies to all the shared admin queries too (users, organizations, audit log, …).

**2. Search results stopped showing what matched. Before the WASM migration the template's contract
(`ICRUDService.GetListAsync`'s doc comment) was that a service wrapped matches in `<mark>…</mark>`
**inside the DTO strings**, and the in-process Blazor Server pages rendered them. The WASM migration
dropped that — correctly, because markup in data fields:

- reaches every consumer of the same service: edit forms, pickers (ThreadIQ's chat context picker had a
  regex to strip the tags), exports, offline caches, AI tool results;
- forces the client to render the fields as raw HTML, so one service that forgets to HTML-encode before
  inserting tags is a stored XSS — and list text includes inbound email;
- breaks the wire rule in `docs/Api.md` that DTOs carry data.

Nothing replaced it, so no list highlights any more. Doing it purely client-side would re-implement each
query's matching in the browser (phone numbers already match by digits, ignoring formatting), and it
would drift as search grows — ThreadIQ plans to move to PostgreSQL full-text search, where only the
server knows what matched. So the server computes **where** the term matched, as positions, and the
client only draws them. All of it is generic grid/list infrastructure, hence `Core`.

## Current state (DevCoreApp, before this change)

| Surface | Behaviour |
|---|---|
| `Shared.Model.Core.Common.PagedList<T>` | `Items`, `Search`, paging, sort. No match information. |
| `Server.Services.ICRUDService` | Doc comment still asks for `<mark>` in item fields — stale since the WASM migration. |
| `Client.Desktop…HDataGrid` | Renders `ValueSelector(item)` as text. No highlight support. |
| Desktop styles | No style for search matches (ThreadIQ had a product-only `mark.tiq-mark`, removed by this change). |

## Proposed change

### 0. Case-insensitive `Search()` in every query

In each query's `Search(string search)`, lower-case the term once and lower-case each column:

```csharp
public IUserProfilesQuery Search(string search)
{
    // Case-insensitive: both sides lower-cased, which every provider (and InMemory) translates.
    var term = search.ToLowerInvariant();
    ...
    where profile.FirstName.ToLower().Contains(term) ||   // was: profile.FirstName.IndexOf(search) >= 0
          ...
          (matchDigits && profile.PhoneNumber.IndexOf(digits) >= 0) ||   // digit match unchanged
```

`IndexOf(search) >= 0` becomes `.ToLower().Contains(term)` as well, so all queries read the same.
Npgsql emits `lower(col) LIKE '%term%'` with LIKE wildcards in the term escaped (checked with
`ToQueryString()`). `EF.Functions.ILike` was rejected: it is Npgsql-only, so the shared queries would no
longer build against the SQL Server provider, and the InMemory provider used by the tests throws on it.
Existing null guards stay. No index serves a leading-wildcard `LIKE` either way; trigram indexes or
full-text search are the follow-up.

The 14 shared queries (`Data/Queries/BasicsImplementation/`): `CoreApiKeyQuery`, `CoreAuditLogQuery`,
`CoreBackgroundTaskQuery`, `CoreEmailLogQuery`, `CoreFeatureFlagQuery`, `CoreFileRecordQuery`,
`CoreImportSessionQuery`, `CoreNotificationQuery`, `CoreNotificationTemplateQuery`,
`CoreOrganizationsQuery`, `CoreSettingsQuery`, `CoreUserLoginHistoryQuery`, `CoreUserProfilesQuery`,
`CoreWebhookSubscriptionQuery`. Any query DevCoreApp has that ThreadIQ lacks gets the same treatment.

### 1. Wire type — `Shared.Model.Core.Common`

New `HighlightRange { int Start; int Length; }` (parameterless + `(start, length)` constructors), offsets in
UTF-16 code units into the field's value **as sent** (after the decorator formats it). It also carries the
pure splitter the clients render with:

```csharp
public static IReadOnlyList<(string Text, bool IsMatch)> Segments(string? text, IEnumerable<HighlightRange>? ranges)
// sorts, clips ranges to the text (a truncated preview can reuse the whole field's ranges),
// merges overlapping/touching ones; concatenating the segments gives back the text.
```

`PagedList<T>` gains:

```csharp
/// item Id → DTO property name (C# spelling, e.g. "FullName") → ranges in that property's value.
/// Null when not searched or the service computes none; items/fields without a match are absent.
public Dictionary<string, Dictionary<string, HighlightRange[]>> Highlights { get; set; }
```

and the static `PagedList` class gains `HighlightsFor(this PagedList<T> list, string itemId, string field)`
(null-safe; returns null when there is nothing). System.Text.Json does not camel-case dictionary keys
under the web defaults, so `nameof(Item.Prop)` on the client finds them — pinned by a test.

### 2. Server — `Server.Services.Core.Search.SearchHighlighter` (new folder `Core/Search/`)

```csharp
list = PagedList.Create(items, totalCount, top, page, sortBy, search)
    .WithHighlights(search, h => h
        .Text(nameof(ContactItem.FullName), c => c.FullName)
        .Phone(nameof(ContactItem.Phone), c => c.Phone));   // literal term + its digits (≥ 3)
```

- `WithHighlights` is a no-op for a null/empty search; keys rows by `ModelItem.Id`; skips rows and
  fields with no match; leaves `Highlights` null when nothing matched.
- `SearchHighlightBuilder<T>`: `Text(field, selector)`, `Phone(field, selector, minDigits = 3)`,
  `Custom(field, selector, find)`, and `Comparison` (default **`OrdinalIgnoreCase`**, matching the
  case-insensitive `Search()` from step 0 — the highlight must agree with the search).
- Public helpers `FindText(text, term, comparison = OrdinalIgnoreCase)` (all non-overlapping occurrences) and
  `FindDigits(text, digits)` (matches a digit sequence across separators: `5551234` inside `(555) 123-4567`).
- **Rule:** configure it in the service's list method next to the `query.Search(search)` call, highlight
  only fields that query searches, and change both together. ThreadIQ also put a one-line comment on
  each query's `Search()` pointing at its highlighter.

`ICRUDService.GetListAsync`'s doc comment: replace the `<mark>` sentence with "item fields carry plain
values, never markup; a service that wants the client to highlight search matches sets
`PagedList.Highlights` (see `SearchHighlighter`)".

### 3. Desktop client

- **`Core/UI/Components/SearchHighlight.cs`** — a `ComponentBase` with `[Parameter] string? Text` and
  `[Parameter] IEnumerable<HighlightRange>? Ranges`; `BuildRenderTree` emits text nodes and
  `<mark class="search-mark">` per `HighlightRange.Segments`. Deliberately a `.cs` class, not `.razor`:
  Razor markup puts whitespace text nodes between segments, which show inside `white-space: pre-wrap`.
  Content is never rendered as markup, so it is safe for any text.
- **`ColumnDescriptor<TItem>.HighlightField`** (`string?`, init) — the DTO property whose ranges apply to
  the column's value.
- **`HDataGrid`** — in both default-cell branches (grouped and ungrouped), before the plain
  `@col.ValueSelector(item)` fallback:
  ```razor
  else if (col.HighlightField != null)
  {
      <SearchHighlight Text="@(col.ValueSelector(item)?.ToString())" Ranges="@HighlightsFor(item, col.HighlightField)" />
  }
  ```
  with, in the code-behind,
  ```csharp
  private HighlightRange[]? HighlightsFor(TItem item, string field) =>
      Data.HighlightsFor(IdSelector?.Invoke(item) ?? item.Id, field);
  ```
  Templated cells call the component themselves:
  `<SearchHighlight Text="@item.Name" Ranges="@List.HighlightsFor(item.Id, nameof(MyItem.Name))" />`.
- **`Styles/scss/_components.scss`** — append:
  ```scss
  mark.search-mark {
      background: var(--accent-bg, #fff3cd);
      color: var(--accent, inherit);
      padding: 0;
      border-radius: 3px;
  }
  ```
  The fallbacks cover a theme without ThreadIQ's `--accent-bg`/`--accent` tokens; rebuild so
  `wwwroot/app.css` regenerates.
- **`HDataGrid.md`** — the new "Search Highlighting" section and the `HighlightField` line in the
  `ColumnDescriptor` listing.

### 4. Contract doc — `docs/Api.md` "Lists and queries"

Add `"highlights": null` to the `PagedList` JSON sample and the paragraph describing the shape (keys,
C#-named fields, UTF-16 ranges, no markup in items, built by `SearchHighlighter`, rendered by
`HighlightsFor` + `<SearchHighlight>`). Take the ThreadIQ text verbatim.

No service in DevCoreApp needs to compute highlights for this to build; `Highlights` stays null and
every page renders as before. Wiring each list is per-service opt-in.

## Affected shared surface

| Sync key | Change |
|---|---|
| `Server.Database.Core.Data.Queries.Core*Query` (14, listed in step 0) | `Search()` case-insensitive |
| `Shared.Model.Core.Common.HighlightRange` | new |
| `Shared.Model.Core.Common.PagedList` | `Highlights`, `HighlightsFor`, `using System.Collections.Generic` |
| `Server.Services.Core.Search.SearchHighlighter` | new (file also holds `SearchHighlightBuilder<T>`) |
| `Server.Services.ICRUDService` | doc comment |
| `Client.Desktop.Core.UI.Components.SearchHighlight` | new |
| `Client.Desktop.Core.UI.Components.HDataGrid` | `.razor`, `.razor.cs`, `.md` |
| `Client.Desktop.Core.UI.Model.Grid.ColumnDescriptor` | `HighlightField` |
| Desktop `Styles/scss/_components.scss` (+ generated `wwwroot/app.css`) | `mark.search-mark` |
| `docs/Api.md` | highlights paragraph + sample |
| tests: `Server.Tests.Core.Search.SearchHighlighterTests`, `Client.Services.Tests.Core.HighlightRangeTests` | new |

None of these carry a `project-specific` fence in ThreadIQ, and the change adds none.

## Source change

```bash
git show <change> -- src/Server/Database/Core/Data/Queries/BasicsImplementation src/Shared/Model/Core src/Server/Services/Core/Search src/Server/Services/ICRUDService.cs \
  src/Client/ThreadIQ.Client.Desktop/Core src/Client/ThreadIQ.Client.Desktop/Styles/scss/_components.scss \
  docs/Api.md tests/Server/Api/Core/Search tests/Client/Client.Services.Tests/Core/HighlightRangeTests.cs
```

That filter should list **27 files**: the 14 queries from step 0, `HighlightRange.cs`, `PagedList.cs`, `SearchHighlighter.cs`,
`ICRUDService.cs`, `SearchHighlight.cs`, `HDataGrid.razor`, `HDataGrid.razor.cs`, `HDataGrid.md`,
`ColumnDescriptor.cs`, `_components.scss`, `Api.md` and the two test files. The generated
`wwwroot/app.css` is left out on purpose: rebuild to regenerate it. This doc ships inside the commit
it names, so that commit still has the placeholder in `change`; read the doc from `main`.

Do **not** take: `src/Server/Database/Core/App/**` (the 9 product queries got the same `Search()`
change, plus a comment pointing at their highlighter), `src/Server/Services/App/CRM/*` (the Contact/Company/Interaction services wiring their
highlights), `src/Client/**/App/**`
(the CRM pages), `Styles/scss/_crm.scss` (removal of ThreadIQ's product `mark.tiq-mark`),
`tests/Server/Api/App/Services/CrmSearchCaseTests.cs` (runs CRM queries through ThreadIQ's in-memory
CRM harness — write the Core equivalent against a shared query, e.g. users or organizations),
`docs/open-items.md`, `src/Server/Database/Core/CLAUDE.md` (gained a short "Search" section; take it
if DevCoreApp carries the area guides), this doc, and anything else under `docs/`. They show how a product list opts in —
read them as the worked example. The diff is a reference, not a patch: every namespace carries a
different product prefix in DevCoreApp.

## Verification

- `dotnet build` (the Desktop build recompiles SCSS; check `mark.search-mark` is in `wwwroot/app.css`).
- `dotnet test --filter "FullyQualifiedName~SearchHighlighter|FullyQualifiedName~HighlightRange"` —
  12 tests: occurrences, non-overlap, case-insensitive default, digits across formatting, phone digit guard,
  keying/skip rules, null search, segment merge/clip, and the web-JSON round trip with C#-named keys.
- `ApiContractTests` still pass (the field is additive).
- Add a query test: seed a row named "Plain", search "PLain" through the real query over InMemory,
  expect it found (ThreadIQ's `CrmSearchCaseTests` is the model).
- Manual: search an admin list (Users, Organizations) in a different case from the stored value.
- Manual: opt one list in (e.g. Users — see Open questions), search, and confirm matched text is tinted
  in plain and templated cells, and that a term with `<b>` in it renders literally.

## Open questions

- **Users list.** The template's `UserProfileService.GetListAsync` passes `useSearchMarkup: true` to
  `PagedList.Create`, a leftover of the old `<mark>` contract. Check what WebServiceToolkit does with the
  flag; it is the natural first Core list to opt in to `WithHighlights`, and ThreadIQ has not done it yet.
- **Index support.** `lower(col) LIKE '%term%'` scans; at template volumes that is fine. ThreadIQ
  plans full-text search, at which point a query's highlighter switches to a `Custom` matcher
  (PostgreSQL's `ts_headline` can supply positions) in the same commit.
