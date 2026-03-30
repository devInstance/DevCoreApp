# Background Tasks

## Overview

The background task system provides asynchronous job execution for work that should not block the main request pipeline. In the current starter implementation, it supports:

- persisted background jobs in the database
- pluggable task handlers by task type
- configurable parallel execution
- delayed retry with exponential backoff
- startup and periodic stale-task recovery
- admin visibility into jobs and execution attempts
- health checks for worker heartbeat and queue state

This is the main mechanism used by the starter for asynchronous work such as email delivery, import processing, and webhook delivery.

## Why This Feature Exists

The starter includes a background job system so application code can queue work quickly and return control to the user without waiting for slow external operations.

Typical uses in the current codebase include:

- sending emails
- processing imports
- delivering webhooks

## High-Level Design

The system is split across job submission, persisted task records, worker execution, per-type handlers, and admin monitoring.

| Area | Responsibility | Main files |
|---|---|---|
| Submission API | Persists jobs and enqueues immediate processing | `src/Server/Admin/Services/Background/BackgroundWorker.cs` |
| Worker engine | Claims, runs, retries, and recovers tasks | `src/Server/Admin/Services/Background/Tasks/BackgroundTaskWorker.cs` |
| Settings | Configurable concurrency, polling, retry, and recovery behavior | `src/Server/Admin/Services/Background/Tasks/BackgroundTaskSettings.cs` |
| Task handlers | Implements actual job logic by task type | `src/Server/Admin/Services/Background/Tasks/Handlers/*.cs` |
| Data model | Stores jobs and per-attempt logs | `src/Server/Database/Core/Models/BackgroundTasks/BackgroundTask.cs`, `src/Server/Database/Core/Models/BackgroundTasks/BackgroundTaskLog.cs` |
| Query/admin service | Lists and manages jobs in the admin app | `src/Server/Admin/Services/BackgroundTasks/JobDashboardService.cs` |
| Admin UI | Job dashboard and per-job logs | `src/Server/Admin/WebService/UI/Pages/Admin/JobDashboardPage.razor` |
| Health checks | Reports worker heartbeat and queue state | `src/Server/Admin/WebService/Health/BackgroundWorkerHealthCheck.cs` |

## Core Flow

The runtime flow is:

1. application code submits a `BackgroundRequestItem`
2. `BackgroundWorker.SubmitAsync` persists a `BackgroundTask` row
3. the task id is pushed into an in-memory immediate queue
4. `BackgroundTaskWorker` drains immediate ids and also polls the database for due queued jobs
5. each candidate job is atomically claimed by changing its DB status from `Queued` to `Running`
6. the worker dispatches the job to the matching `IBackgroundTaskHandler`
7. success marks the job `Completed`
8. failure creates a failed attempt log and either requeues the job with delay or marks it `Failed`

The important design point is that the database is the source of truth. The in-memory queue is only a local wake-up optimization.

## Data Model

### BackgroundTask

`BackgroundTask` stores the persisted job record.

```csharp
public class BackgroundTask : DatabaseObject, IOrganizationScoped
{
    public Guid OrganizationId { get; set; }
    public string TaskType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public BackgroundTaskStatus Status { get; set; }
    public int Priority { get; set; }
    public int RetryCount { get; set; }
    public int MaxRetries { get; set; }
    public string? ResultReference { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime ScheduledAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? CronExpression { get; set; }
    public Guid CreatedById { get; set; }
}
```

### Field meanings

| Field | Meaning |
|---|---|
| `TaskType` | Logical job type routed to a handler |
| `Payload` | Serialized request body, stored as `jsonb` |
| `Status` | `Queued`, `Running`, `Completed`, or `Failed` |
| `Priority` | Lower values run first |
| `RetryCount` | Number of failed attempts already consumed |
| `MaxRetries` | Total allowed attempts before terminal failure |
| `ResultReference` | Link to another record such as `EmailLog:{id}` or `WebhookDelivery:{id}` |
| `ErrorMessage` | Last known failure message |
| `ScheduledAt` | When the task becomes eligible to run |
| `StartedAt` | When a worker claimed the task |
| `CompletedAt` | When the task reached a terminal state |

### BackgroundTaskLog

Each execution attempt gets its own `BackgroundTaskLog` row. This is what powers the per-job attempt history in the admin UI.

Each attempt log records:

- attempt number
- status
- start time
- completion time
- error message

## EF Core Configuration

`ApplicationDbContext` configures:

- `Payload` as `jsonb`
- FK to `CreatedBy`
- FK to `Organization`
- indexes on:
  - `Status`
  - `TaskType`
  - `ScheduledAt`
  - `OrganizationId`

These indexes are important because the worker continuously filters by status and due schedule time.

## Task Submission

The application-facing entry point is `IBackgroundWorker`.

```csharp
public interface IBackgroundWorker
{
    Task SubmitAsync(BackgroundRequestItem item);
    void Submit(BackgroundRequestItem item);
    DateTime? LastHeartbeat { get; }
    int QueueLength { get; }
}
```

### Preferred API

`SubmitAsync` is the preferred path. It persists the job and then enqueues the task id for immediate pickup.

`Submit` is a legacy wrapper that kicks off `SubmitAsync` via `Task.Run`. It still works, but new code should prefer `SubmitAsync`.

### Built-in request types

The current request-type mapping is:

| Request type | Task type |
|---|---|
| `SendEmail` | `BackgroundTaskTypes.SendEmail` |
| `ImportData` | `BackgroundTaskTypes.ImportData` |
| `DeliverWebhook` | `BackgroundTaskTypes.DeliverWebhook` |

### Result references

Some jobs store a link to a related record in `ResultReference`.

Current patterns include:

- `EmailLog:{publicId}`
- `ImportSession:{publicId}`
- `WebhookDelivery:{publicId}`

This is how the system ties a job back to another operational record.

## Configurable Parallelism

Yes, parallel execution is already configurable.

`BackgroundTaskSettings` currently exposes:

```csharp
public class BackgroundTaskSettings
{
    public int MaxConcurrency { get; set; } = 4;
    public int PollingIntervalSeconds { get; set; } = 10;
    public int BaseRetryDelaySeconds { get; set; } = 30;
    public int MaxRetryDelaySeconds { get; set; } = 3600;
    public int BatchSize { get; set; } = 10;
    public int RunningTaskTimeoutMinutes { get; set; } = 15;
    public int RecoverySweepIntervalSeconds { get; set; } = 60;
}
```

The worker creates a semaphore using `MaxConcurrency`, so this is the primary knob that controls how many jobs can run in parallel inside one app instance.

### Current default settings

`appsettings.json` now includes an explicit `BackgroundTasks` section:

```json
"BackgroundTasks": {
  "MaxConcurrency": 4,
  "PollingIntervalSeconds": 10,
  "BaseRetryDelaySeconds": 30,
  "MaxRetryDelaySeconds": 3600,
  "BatchSize": 10,
  "RunningTaskTimeoutMinutes": 15,
  "RecoverySweepIntervalSeconds": 60
}
```

Development config shortens some timings for faster feedback.

### What each setting does

| Setting | Meaning |
|---|---|
| `MaxConcurrency` | Maximum number of tasks this app instance processes at once |
| `PollingIntervalSeconds` | Sleep time when no jobs are available |
| `BaseRetryDelaySeconds` | Initial retry delay before exponential backoff |
| `MaxRetryDelaySeconds` | Cap on retry backoff delay |
| `BatchSize` | Maximum due queued rows loaded from the DB per poll |
| `RunningTaskTimeoutMinutes` | Age at which a running task is considered stale |
| `RecoverySweepIntervalSeconds` | How often the worker scans for stale running tasks |

## Claiming and Execution Model

The worker uses two sources of candidate jobs:

- the local immediate queue
- the database queue

### Important recent safety behavior

The current implementation no longer treats immediate-queue ids as already claimed work. Instead, it merges immediate ids and DB ids into one candidate set and then claims each one through the database with:

- `WHERE Id = ... AND Status = Queued`
- update to `Running`

That matters because it prevents duplicate execution of the same task in the same loop when a task exists both in local memory and in the database query result.

### Status guard before execution

`ProcessTaskAsync` now checks that the loaded job is actually `Running` before it executes the handler. If the row is no longer in `Running`, execution is skipped.

This is another correctness guard that becomes more important as concurrency increases or if you ever run multiple nodes.

## Retry Behavior

When a handler throws:

1. the current attempt log is marked failed
2. `RetryCount` is incremented
3. if retries remain, the task is put back into `Queued`
4. `ScheduledAt` is moved into the future using exponential backoff
5. otherwise the task is marked `Failed`

### Backoff formula

The retry delay is:

```text
BaseRetryDelaySeconds * 2^(RetryCount - 1)
```

clamped to `MaxRetryDelaySeconds`.

### Per-type retry limits

`BackgroundWorker.GetMaxRetries` currently sets:

| Request type | Max retries |
|---|---|
| `SendEmail` | `1` |
| `DeliverWebhook` | `5` |
| other types | `3` |

The email path is intentionally conservative to reduce duplicate-send risk.

## Stale Running Task Recovery

The worker now recovers stale `Running` tasks in two places:

- once at startup
- periodically while the worker is running

### Recovery rule

A task is considered stale when:

- `Status == Running`
- and `StartedAt` is missing or older than `RunningTaskTimeoutMinutes`

Recovered tasks are reset to:

- `Status = Queued`
- `StartedAt = null`
- `ScheduledAt = now`

### Why this matters

This makes the system safer if:

- the process crashes mid-task
- a node dies while other nodes stay alive
- a handler hangs long enough to exceed the configured running timeout

This is still a relatively simple recovery model. It is not a distributed lease system, but it is much safer than only resetting `Running` rows on process startup.

## Current Scale Characteristics

### What already scales reasonably well

- multiple tasks can run in parallel in one instance via `MaxConcurrency`
- due jobs are persisted in the DB, so work survives restarts
- claim operations are atomic at the row level
- stale-running recovery helps keep abandoned work from being stuck forever

### What is still intentionally simple

- there is no distributed lease token per task
- there is no handler-specific concurrency limit
- there is no per-task-type worker pool
- there is no rate limiting for external systems
- there is no priority partitioning beyond numeric sort order
- there is no recurring-job scheduler despite the presence of `CronExpression`

For the starter, that is a reasonable tradeoff. It keeps the model understandable while still being tunable.

## Built-In Task Handlers

Current built-in handlers are:

- `SendEmailTaskHandler`
- `ImportDataTaskHandler`
- `WebhookDeliveryTaskHandler`

Each handler advertises a `TaskType` string and implements `HandleAsync(payload, scopedProvider, cancellationToken)`.

### How to add a new task type

To add a new background task:

1. create a request payload model
2. create a handler implementing `IBackgroundTaskHandler`
3. give the handler a unique `TaskType`
4. register the handler in DI in `Program.cs`
5. update the request-type mapping in `BackgroundWorker` if you want a typed `BackgroundRequestType`
6. submit the request through `IBackgroundWorker`

## Admin UI

The job dashboard is available at:

```text
/admin/jobs
```

### Current capabilities

The admin UI supports:

- listing jobs
- filtering by status
- filtering by task type
- sorting
- viewing selected job details
- viewing execution attempt logs
- cancelling queued jobs
- retrying failed jobs

### Important job-management behavior

- only `Queued` jobs can be cancelled
- only `Failed` jobs can be retried
- retrying a failed job creates a new queued job row rather than mutating the old failed row back into a queued state

That keeps failure history intact.

## Health Checks

The system registers a background-worker readiness check.

The health check now reports:

- local immediate queue length
- total queued DB task count
- due queued DB task count
- running DB task count
- stale running DB task count
- last worker heartbeat

### Health semantics

- `Degraded` if the worker has not started yet
- `Unhealthy` if the heartbeat is stale
- `Degraded` if stale running tasks are detected
- `Healthy` otherwise

This is more useful than a local-memory-only signal because the database queue is the real operational backlog.

## Operational Guidance

### If you want to stay conservative

Use:

- modest `MaxConcurrency`
- moderate `BatchSize`
- a realistic running timeout

This is a good fit for a starter where tasks involve external systems and you want predictable behavior more than maximum throughput.

### If you need more throughput

Increase `MaxConcurrency` first.

Then review:

- whether handlers are thread-safe
- whether external systems can tolerate the increased parallelism
- whether `BatchSize` should be raised to keep workers fed
- whether retry delays are still appropriate at higher traffic volumes

### If you run multiple app instances

The current model can work across multiple instances because claims are DB-based, but it is still not a full distributed-job platform.

Be especially careful about:

- long-running handlers
- duplicate effects in non-idempotent handlers
- recovery timeout values that are too short

## Current Limitations

The most important limits in the current starter are:

### 1. No lease token or worker ownership model

Tasks are protected by status transitions, not by explicit worker leases.

### 2. Recovery is timeout-based

If `RunningTaskTimeoutMinutes` is set too low, a legitimately long-running task can be requeued while still executing.

### 3. `QueueLength` is local-only

`IBackgroundWorker.QueueLength` still reflects only the in-memory immediate queue. The health check compensates for this by querying the DB, but the raw property itself is not a full backlog metric.

### 4. `CronExpression` is not active

The field exists on `BackgroundTask`, but the current worker does not implement recurring scheduling behavior.

### 5. No task-type-specific concurrency controls

All handlers share the same `MaxConcurrency` budget.

## Recommended Developer Setup

For a new project using this starter:

1. leave `MaxConcurrency` low to moderate at first
2. set a realistic `RunningTaskTimeoutMinutes` based on the slowest legitimate handler
3. verify all task handlers are safe under parallel execution
4. make external side effects idempotent where possible
5. use the admin jobs page regularly during development to inspect retries and failures
6. monitor `/health/ready` instead of relying only on local worker metrics
7. increase concurrency only after validating downstream systems can absorb the load

## Summary

The background task system is already configurable for parallel execution and is in a solid place for a starter codebase. Its main strengths are:

- persisted queue state
- configurable parallelism
- simple retry model
- admin visibility
- improved stale-task recovery

Its main tradeoffs are:

- intentionally simple distributed coordination
- no advanced scheduling model
- shared concurrency budget across all task types

For most starter-project workloads, this is a pragmatic balance between simplicity and scalability.
