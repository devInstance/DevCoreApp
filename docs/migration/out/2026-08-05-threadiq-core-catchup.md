---
origin: DevCoreApp
targets: [ThreadIQ]
scope:
  - Server.Admin.Services.Background.Tasks.BackgroundTaskWorker
  - Server.Admin.Services.Background.Tasks.BackgroundTaskSettings
  - Server.Admin.Services.Authentication.PermissionClaimsTransformation
  - Server.Database.Core.Data.Queries.CoreQueryRepository
  - Server.Database.Core.Data.IQueryRepository
status: pending
related:
  - docs/migration/out/2026-08-05-org-scoping-writes-and-entity-stamping.md
  - src/Server/Database/UnitOfWork.md
  - CLAUDE.md ("Data Access Pattern", "Things To Never Do")
---

# Suggestions for ThreadIQ: catch up to canonical `Core`

## Why

A comparison of every shared `Core` surface between the two repos (namespace prefix normalized)
showed the drift is mostly **one-directional**: ThreadIQ is behind DevCoreApp on two refactors that
have since landed upstream, and several ThreadIQ files still carry the older shape. The genuine
ThreadIQ *fixes* have been extracted into
[`2026-08-05-org-scoping-writes-and-entity-stamping.md`](2026-08-05-org-scoping-writes-and-entity-stamping.md)
and applied to canonical `Core`; this doc covers the opposite direction.

Nothing here is urgent in the sense of "production is broken" — it is drift that will make every
future migration doc harder to apply cleanly. Item 1 is the exception: it is a behavior regression
worth fixing on its own merits.

## Current state

Two upstream refactors ThreadIQ has not taken:

| Refactor | DevCoreApp commit | What it changed |
|---|---|---|
| Query classes instead of `DbContext` | `a02224b` | Services and infrastructure reach data only through `IQueryRepository.Get…Query(…)`; injecting `ApplicationDbContext` into a service is now a documented "never do" |
| Per-operation unit of work | `c3727b3` | `BaseService.Repository` removed; each method opens `await using var repo = RepositoryFactory.Create()` |

ThreadIQ files still on the old shape include `BackgroundTaskWorker`, `PermissionClaimsTransformation`,
and parts of `SettingsService` / `JwtAuthService`, all of which resolve `ApplicationDbContext`
directly from the scope.

## Proposed change

### 1. Restore periodic stale-task recovery (behavior regression)

`BackgroundTaskWorker.RecoverStuckTasksAsync` currently runs **once at startup** and resets *every*
row in `Running` back to `Queued`, unconditionally:

```csharp
await db.BackgroundTasks
    .Where(t => t.Status == BackgroundTaskStatus.Running)
    .ExecuteUpdateAsync(…);
```

Two problems:

- **It only runs at startup.** A task whose process dies mid-flight during normal operation stays
  `Running` forever; nothing sweeps it up until the next restart.
- **It has no age cutoff.** With more than one instance (or a rolling deploy), a starting instance
  resets tasks another instance is actively running, so that work executes twice.

Canonical `Core` sweeps on a timer with an age cutoff, driven by two settings ThreadIQ has dropped
from `BackgroundTaskSettings`:

```csharp
public int RunningTaskTimeoutMinutes { get; set; } = 15;
public int RecoverySweepIntervalSeconds { get; set; } = 60;
```

…and recovers only rows older than the cutoff, via
`GetBackgroundTaskQuery(null!).RecoverStuckRunningAsync(timeoutCutoff, now)`, tracking
`_lastRecoverySweepUtc` so the sweep is rate-limited rather than run every poll.

Restore both settings and the `RecoverStaleRunningTasksIfDueAsync` path.

### 2. Restore per-request-type retry counts

`BackgroundWorker` currently hardcodes `task.MaxRetries = 3`. Canonical `Core` varies it by request
type, which matters: retrying a send five times is how you send five emails.

```csharp
private static int GetMaxRetries(BackgroundRequestType requestType) => requestType switch
{
    BackgroundRequestType.SendEmail => 1,
    BackgroundRequestType.DeliverWebhook => 5,
    _ => 3
};
```

Keep ThreadIQ's product request types in the switch; only the default arms come from upstream.

### 3. Move `BackgroundTaskWorker` onto query classes

Replace the direct `ApplicationDbContext` usage with `IQueryRepository` and the
`IBackgroundTaskQuery` methods upstream added for exactly this: `SelectQueuedCandidateIdsAsync`,
`TryClaimAsync`, `RecoverStuckRunningAsync`, `FindByIdAsync`. This is what makes items 1 and 2
mergeable without conflict, and brings the worker in line with the "never call `DbContext` directly
from a service" rule.

Note that upstream's `ClaimQueuedTasksAsync` **already routes immediate-queue ids through the same
atomic claim** — the double-claim bug ThreadIQ found and fixed does not exist there. Your fix and
the upstream shape agree; only the data-access layer differs.

### 4. `CreateAsyncScope` is not needed upstream — confirm which fix you want

ThreadIQ switched several `CreateScope()` calls to `CreateAsyncScope()` because `IQueryRepository` is
`IAsyncDisposable`-only and the container refuses to dispose such a service from a sync scope.
Canonical `Core` solved the same problem at the other end: `CoreQueryRepository` implements **both**
`IDisposable` and `IAsyncDisposable`, with both paths no-ops unless the repository owns its context.

Either fix works; they are not in conflict, and adopting `CoreQueryRepository` as-is makes the
`CreateAsyncScope` changes unnecessary rather than wrong. Keeping both is fine. What is *not* fine is
adopting upstream's `CoreQueryRepository` while assuming the sync-dispose problem is still live.

### 5. Decide the API-key permission semantics deliberately

The two repos disagree, and neither is strictly safer:

| | DevCoreApp | ThreadIQ |
|---|---|---|
| Principal has `ApiKeyId` | Early return granting **exactly** the key's scopes; roles ignored | Full role + override set, then **intersected** with the key's scopes |
| Key exceeding the user's own permissions | Possible | Impossible |
| Key with null/empty scopes | Grants **nothing** | Applies **no restriction** — grants the user's full set |

ThreadIQ's intersection model is the better default (a key should never exceed its owner), but the
empty-scopes hole is a real escalation path: a key created without scopes currently inherits
everything its owner can do. At minimum, treat null/empty scopes as "no permissions" rather than "no
restriction".

This one is a genuine design decision, not drift. Whichever way it goes should land in canonical
`Core` and fan out, rather than staying divergent.

## Affected shared surface

- `Server.Admin.Services.Background.Tasks.BackgroundTaskWorker` — items 1, 3, 4
- `Server.Admin.Services.Background.Tasks.BackgroundTaskSettings` — item 1 (two settings)
- `Server.Admin.Services.Background.BackgroundWorker` — item 2
- `Server.Admin.Services.Authentication.PermissionClaimsTransformation` — item 5
- `Server.Database.Core.Data.{IQueryRepository, Queries.CoreQueryRepository}` — items 3, 4

**Collision risk is high in `BackgroundTaskWorker` and `BackgroundWorker`** — ThreadIQ has many
product task types (calendar sync, mailbox sync, email normalization) threaded through both. Merge
by hand; do not overwrite. If any of it must diverge permanently, fence it with
`#region project-specific ThreadIQ: <reason>` so the next migration doc leaves it alone.

## Verification

```bash
dotnet build ThreadIQ.slnx
dotnet test  ThreadIQ.slnx
```

For item 1, the behavior to confirm: start a task, kill the process mid-run, and check that the row
returns to `Queued` **without** a restart once `RunningTaskTimeoutMinutes` has elapsed — and that a
second instance starting up does **not** reset a task the first is actively running.

## Open questions

- Item 5 needs a decision from you before either repo changes. Once made, it should travel as its own
  migration doc in both directions.
- ThreadIQ has reorganized query interfaces into `Queries/Admin/` and
  `Queries/BasicsImplementation/Admin/` subfolders. Canonical `Core` keeps them flat. This is
  cosmetic, but it changes file paths for every future doc — worth agreeing on one layout. The
  `Core`/`App` split in `CLAUDE.md` would suggest `Core/` and `App/` segments rather than `Admin/`.
