---
origin: ThreadIQ
targets: [DevCoreApp]
scope:
  # Ports — new projects. Sync keys below are relative to each project root namespace.
  - Server.Integrations.Email.Core.*                 # new — EmailIntegration.Abstractions.csproj
  - Server.Integrations.Email.ConfigurationExtensions # new — root, unmarked
  - Server.Integrations.Calendar.Core.*              # new — CalendarIntegration.Abstractions.csproj
  - Server.Integrations.Calendar.ConfigurationExtensions # new — root, unmarked
  # Microsoft Graph adapters — new projects
  - Server.Integrations.Email.Graph.Core.*           # new — EmailIntegration.Graph.csproj
  - Server.Integrations.Email.Graph.ConfigurationExtensions # new — root, unmarked (+ AssemblyInfo.cs)
  - Server.Integrations.Calendar.Graph.Core.*        # new — CalendarIntegration.Graph.csproj
  - Server.Integrations.Calendar.Graph.ConfigurationExtensions # new — root, unmarked (+ AssemblyInfo.cs)
  # Tests — new projects
  - Server.Integrations.Email.Graph.Tests.Core.*     # new — EmailIntegration.Graph.Tests.csproj
  - Server.Integrations.Calendar.Graph.Tests.Core.*  # new — CalendarIntegration.Graph.Tests.csproj
  - <solution file>                                  # four src + two test project entries
status: pending
change: "b912478274d39026c17d560c1e43aaeb670ef2f0"   # ThreadIQ dev/1192-integrations-to-core, "#1192: That makes them a reasonable candidate to send upstream later"
related:
  - docs/email-integration-plan.md        # ThreadIQ-local: why the port is shaped the way it is
  - docs/calendar-integration-plan.md     # ThreadIQ-local: the calendar half
  - docs/email-integration-remaining-work.md  # ThreadIQ-local: live-mailbox status
---

# Inbound mail + calendar capture ports and their Microsoft Graph adapters move into shared Core

## Delivery & audit log — DevCoreApp

| Date | Event |
|---|---|
| 2026-10-07 | Authored in ThreadIQ `out/` under `#1192`. Not yet delivered. |
| 2026-10-07 | ThreadIQ change committed as `b912478`. `change` filled in by the next commit, so that SHA carries this doc with the placeholder — read the doc from the branch tip, not from the commit it names. |

## Why

ThreadIQ built a vendor-neutral port for **reading** a user's mailbox and calendar (`IMailProvider`,
`ICalendarProvider`) plus a Microsoft Graph adapter for each. None of that code knows about the CRM:
it connects to a mailbox over OAuth, pages through a delta feed with a watermark, searches by
participant, and maps Graph payloads to plain model types. Everything CRM-specific — matching mail
to deals, storing interactions and meetings, the connection entities, the credential store, the
controllers and pages — sits in ThreadIQ's `App/` consumers and stays there.

Any product built on the template that wants "capture mail/meetings from a connected Microsoft 365
mailbox" needs exactly this layer, and the vendor boundary it enforces is template-grade. ThreadIQ
is also about to extend these projects (more `#1192` work follows), so ownership is settled first:
from this change on, fixes to them travel through `docs/migration/` like any other shared code
rather than diverging between repos.

Before this change the four projects carried **no** marker. By the sync rule (`.App.` means local,
everything else is shared) they were technically shared already, but nothing said so, and their
layout did not match the Email/Storage processor projects that came from DevCoreApp. They now do.

## Current state (ThreadIQ, after this change)

```
src/Server/Integrations/
├── Email/
│   ├── Abstractions/   EmailIntegration.Abstractions.csproj   root ns …Server.Integrations.Email
│   │   ├── ConfigurationExtensions.cs      AddEmailIntegration()             (root, unmarked)
│   │   └── Core/                           IMailProvider, IMailProviderFactory, IMailProviderResolver,
│   │                                       MailProviderResolver, IMailCredentialStore, MailOptions,
│   │                                       MailMessage, MailWatermark, MailSearchQuery, …
│   └── Graph/          EmailIntegration.Graph.csproj          root ns …Server.Integrations.Email.Graph
│       ├── ConfigurationExtensions.cs      AddGraphMailProvider()            (root, unmarked)
│       ├── AssemblyInfo.cs                 InternalsVisibleTo(<test assembly>)
│       └── Core/                           GraphMailProvider(+Factory) public; client, mapper,
│                                           token client, cursor, retry policy, notification parser internal
└── Calendar/
    ├── Abstractions/   CalendarIntegration.Abstractions.csproj   (same shape: AddCalendarIntegration())
    └── Graph/          CalendarIntegration.Graph.csproj          (same shape: AddGraphCalendarProvider())

tests/Server/Integrations/
├── EmailIntegration.Graph.Tests/Core/     68 tests
└── CalendarIntegration.Graph.Tests/Core/  17 tests
```

Namespaces follow the processor precedent exactly (`…Server.EmailProcessor.Core`,
`…Server.EmailProcessor.MailKit.Core`):

| File | Namespace |
|---|---|
| `Email/Abstractions/Core/*.cs` | `DevInstance.<Product>.Server.Integrations.Email.Core` |
| `Email/Graph/Core/*.cs` | `DevInstance.<Product>.Server.Integrations.Email.Graph.Core` |
| `Calendar/Abstractions/Core/*.cs` | `DevInstance.<Product>.Server.Integrations.Calendar.Core` |
| `Calendar/Graph/Core/*.cs` | `DevInstance.<Product>.Server.Integrations.Calendar.Graph.Core` |
| each project's `ConfigurationExtensions.cs` | the project root namespace (no `.Core`) |
| tests under `…Tests/Core/` | `DevInstance.<Product>.Server.Integrations.{Email,Calendar}.Graph.Tests.Core` |

`<RootNamespace>` and `<AssemblyName>` are unchanged and carry no `Core` segment.

## Proposed change (DevCoreApp)

Add the four projects and the two test projects as they stand in ThreadIQ, with the namespace prefix
swapped to DevCoreApp's. Concretely:

1. **Projects.** Create `src/Server/Integrations/{Email,Calendar}/{Abstractions,Graph}/` with the
   csproj files as in ThreadIQ — `net10.0`, `Nullable` + `ImplicitUsings` enabled,
   `Configurations` = `Debug;Release;ServiceMocks`, `AssemblyName` = `RootNamespace` =
   `DevInstance.<Product>.Server.Integrations.<Email|Calendar>[.Graph]` (the Abstractions assembly
   name additionally ends in `.Abstractions`). Packages:
   - Abstractions: `Microsoft.Extensions.Configuration.Binder` and
     `Microsoft.Extensions.DependencyInjection.Abstractions` (10.0.12). **Nothing else** — no
     provider SDK may be referenced from a port.
   - Graph adapters: `Microsoft.Graph` 6.2.0, `Microsoft.Extensions.Http` 10.0.12, and a direct
     `Microsoft.Kiota.Abstractions` 2.0.0 pin that exists only to lift the vulnerable transitive
     (GHSA-7j59-v9qr-6fq9) — keep its comment, and drop the pin once Microsoft.Graph depends on a
     fixed version. Each adapter references only its own Abstractions project.
2. **Tests.** `tests/Server/Integrations/{EmailIntegration,CalendarIntegration}.Graph.Tests/` —
   xunit.v3, referencing only their adapter. The test **assembly name must match the
   `InternalsVisibleTo` in the adapter's `AssemblyInfo.cs`**; with DevCoreApp's prefix both change
   together, or every test touching an internal type fails to compile.
3. **Solution.** Add the six projects to the solution file (a `/src/Server/Integrations/` folder,
   as in ThreadIQ's `ThreadIQ.slnx`).
4. **No host wiring.** DevCoreApp has no consumer of these ports yet, so do **not** add project
   references from `Server.Services`/`Server.Api` or the `AddEmailIntegration()` /
   `AddGraphMailProvider()` / `AddCalendarIntegration()` / `AddGraphCalendarProvider()` calls to
   `Program.cs`. A product that consumes them adds both; ThreadIQ's `Program.cs` shows the shape
   (resolver first, then each adapter — order does not matter).
5. **CLAUDE.md.** Record the projects in the dependency rules ("Integrations: Abstractions have no
   project deps; each Graph adapter references only its own Abstractions") and note that, with the
   Email/Storage processors, they are `Core/`-only. State that `src/Server/Integrations` reads
   mail (inbound) and is unrelated to `src/Server/Email`, which sends — the names invite confusion.

### Invariants to keep (each is enforced or load-bearing)

- **Vendor types never leave an adapter.** Every Graph/Kiota type is `internal`; the public surface
  of each adapter is exactly `ConfigurationExtensions`, `Graph{Mail,Calendar}Provider` and its
  factory. `PublicSurfaceTests` in each test project pins both rules by reflection — port them
  unchanged.
- **One bad provider entry never takes the host down.** `MailProviderResolver` /
  `CalendarProviderResolver` wrap a failing factory in `Unavailable{Mail,Calendar}Provider` and
  report it, rather than throwing at startup. A duplicate key or unknown `Type` does throw.
- **Configuration.** Both resolvers bind the **same** `Mail` section (`CalendarOptions.SectionName`
  is `"Mail"` on purpose): the calendar has no OAuth of its own and rides the mailbox grant, which
  must include `Calendars.Read`. Shape:
  ```json
  "Mail": {
    "DefaultProvider": "microsoft",
    "Providers": [
      { "Key": "microsoft", "Type": "MicrosoftGraph", "DisplayName": "Microsoft 365", "Enabled": true,
        "ClientId": "...", "ClientSecret": "...", "TenantId": "common",
        "Scopes": [ "offline_access", "https://graph.microsoft.com/Mail.Read",
                    "https://graph.microsoft.com/User.Read" ] }
    ]
  }
  ```
  `Key` is stored on every connected mailbox; changing it orphans connections.
- **`IMailCredentialStore` is a port, not an implementation.** The consumer supplies it (ThreadIQ's
  is an `App/` Data Protection store). Nothing in these projects persists anything.
- **Searches always filter by participant.** `MailSearchQuery` / `CalendarSearchQuery` with an empty
  address list must be rejected by the adapter — the mailbox is never mirrored.
- **Deduplicate events on `ICalUid` + `StartUtc`, never on `ProviderEventId`** (mailbox-scoped).

## Affected shared surface

All new — there is nothing in DevCoreApp's `Core` to collide with, and no `#region project-specific`
fence exists in any of these files. The DevCoreApp-side prefix change touches, besides every
`namespace` line: the two `AssemblyInfo.cs` `InternalsVisibleTo` strings, the six csproj
`AssemblyName`/`RootNamespace` values, and the test projects' `ProjectReference` paths if the folder
depth differs.

## Source change

```bash
git -C <path-to-ThreadIQ> show --stat <change>

# The shared surface only:
git -C <path-to-ThreadIQ> show <change> -- \
    src/Server/Integrations \
    tests/Server/Integrations \
    ThreadIQ.slnx
```

Most of the diff is renames; read the **resulting tree** at that SHA rather than the patch —
`git -C <path-to-ThreadIQ> archive <change> src/Server/Integrations tests/Server/Integrations` gives
the files whole. The diff/tree is a reference, not a patch: every namespace carries a
`DevInstance.ThreadIQ.` prefix where DevCoreApp carries its own.

**ThreadIQ-local — do not take upstream:** `CLAUDE.md`, `docs/` (this document included), and every
consumer touched only to update a `using` — `src/Server/Services/App/{Mail,Calendar}/`,
`src/Server/Api/App/`, `src/Server/Api/Program.cs`, `tests/Server/Api/App/`. Those are the CRM
landing path and the host wiring that item 4 above deliberately leaves out.

Doc comments in the ported files were made product-neutral in the same change (no deal/CRM
references), so they can be taken verbatim.

## Verification

1. `dotnet build` — 0 errors.
2. `dotnet test tests/Server/Integrations/EmailIntegration.Graph.Tests/EmailIntegration.Graph.Tests.csproj`
   → 68 passed; `…/CalendarIntegration.Graph.Tests/CalendarIntegration.Graph.Tests.csproj` → 17
   passed (ThreadIQ counts at `#1192`).
3. `grep -rn 'PackageReference Include="Microsoft.Graph"' --include=*.csproj src` lists only the two
   Graph adapters.
4. `grep -rniE "deal|crm" src/Server/Integrations --include=*.cs` finds nothing.

## Open questions

- **Status of the code.** The adapters are proven only against a faked Graph client (the test
  projects) and ThreadIQ's faked-port service tests; the pipeline has not yet run against a live
  mailbox. DevCoreApp receives them at that maturity.
- **The generic half of the consumer.** Connecting a mailbox (OAuth connect/callback endpoints,
  connect-state protection, a connection entity, a Data Protection `IMailCredentialStore`, per-org
  provider settings) is also product-agnostic, but is currently written against ThreadIQ `App/`
  types. Whether it follows these projects upstream is not decided here; further `#1192` work in
  ThreadIQ may produce a follow-up doc.
