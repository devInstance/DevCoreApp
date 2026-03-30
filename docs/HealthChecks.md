# Health Checks

## Overview

The health check system provides lightweight operational endpoints for liveness and readiness monitoring. In the current starter implementation, it supports:

- a generic liveness endpoint
- a readiness endpoint with dependency checks
- database connectivity monitoring
- background worker heartbeat and queue-state monitoring
- queued email backlog monitoring
- structured JSON responses for external probes

The health system is intended to support local debugging, container orchestration, and deployment monitoring without exposing the full internals of the application.

## Why This Feature Exists

This starter includes built-in health checks so a new project can answer basic operational questions immediately:

- is the web app process alive
- is the database reachable
- is the background worker still running
- are background emails piling up

That gives developers a baseline operational surface without having to build health infrastructure first.

## Registered Checks

The checks are registered in `Program.cs`:

| Check name | Class | Tags |
|---|---|---|
| `database` | `DatabaseHealthCheck` | `ready` |
| `background-worker` | `BackgroundWorkerHealthCheck` | `ready` |
| `stuck-emails` | `StuckEmailsHealthCheck` | `ready` |

There are currently no checks registered without the `ready` tag.

## Endpoints

The web app exposes two health endpoints.

### Liveness

```text
/health
```

This endpoint is configured with:

```csharp
Predicate = _ => false
```

That means it does not execute dependency checks. It only confirms that the web process is up and can respond.

`/health` is intended to stay the minimal public-facing endpoint.

### Readiness

```text
/health/ready
```

This endpoint runs all checks tagged with `ready`, which currently means:

- database connectivity
- background worker status
- stuck email detection

This is the endpoint to use for deployment readiness or operational dashboards.

### Readiness access control

`/health/ready` is no longer broadly public.

The current access rule is:

- local requests are allowed
- non-local requests must include a shared secret header
- if access is not allowed, the endpoint returns `404`

This is handled by `HealthEndpointAccess`, which checks:

- whether the request is loopback or local to the host
- whether the configured header name exists
- whether the header value exactly matches the configured secret

The goal is to keep operational detail available for trusted probes without advertising the readiness surface publicly.

### Readiness security configuration

The access settings are configured through:

```json
"HealthEndpoints": {
  "ReadyHeaderName": "X-Health-Key",
  "ReadySharedSecret": ""
}
```

### Field meanings

| Key | Meaning |
|---|---|
| `HealthEndpoints:ReadyHeaderName` | Header name expected on non-local readiness requests |
| `HealthEndpoints:ReadySharedSecret` | Shared secret value required for non-local readiness requests |

### Practical behavior

If `ReadySharedSecret` is empty:

- local requests to `/health/ready` still work
- non-local requests to `/health/ready` are denied

If `ReadySharedSecret` is configured:

- non-local callers must send the configured header and matching value

Example:

```text
GET /health/ready
X-Health-Key: your-secret-here
```

## Response Format

Responses are written by `HealthCheckResponseWriter` as JSON.

The shape is:

```json
{
  "status": "Healthy",
  "totalDuration": 12.34,
  "checks": [
    {
      "name": "database",
      "status": "Healthy",
      "description": "Database connection is healthy.",
      "duration": 1.23,
      "data": null,
      "exception": null
    }
  ]
}
```

### Field meanings

| Field | Meaning |
|---|---|
| `status` | Aggregate health status across all executed checks |
| `totalDuration` | Total time spent running the health report, in milliseconds |
| `checks` | Per-check results |
| `checks[].name` | Registered check name |
| `checks[].status` | `Healthy`, `Degraded`, or `Unhealthy` |
| `checks[].description` | Human-readable summary |
| `checks[].duration` | Per-check duration in milliseconds |
| `checks[].data` | Optional structured diagnostic data |
| `checks[].exception` | Exception message if the check failed with an exception |

## Database Health Check

`DatabaseHealthCheck` is the simplest check.

### What it does

It calls:

```csharp
_dbContext.Database.CanConnectAsync(...)
```

### Status behavior

- `Healthy` if the app can connect to the configured database
- `Unhealthy` if it cannot connect
- `Unhealthy` if the connectivity test throws

### What it does not do

This check does not validate:

- schema correctness
- migration state
- query latency
- transaction behavior
- specific table availability

It is strictly a connectivity check.

## Background Worker Health Check

`BackgroundWorkerHealthCheck` is the most operationally detailed check in the starter.

### What it measures

It combines two signal sources:

- in-memory worker state from `IBackgroundWorker`
- persisted queue state from the `BackgroundTasks` table

### Data returned

The check currently includes:

- `localImmediateQueueLength`
- `dbQueuedCount`
- `dbDueQueuedCount`
- `dbRunningCount`
- `dbStaleRunningCount`
- `lastHeartbeat`
- `elapsedSeconds`

### Status behavior

- `Degraded` if the worker has not started yet
- `Unhealthy` if the worker heartbeat is stale
- `Degraded` if stale running tasks are detected
- `Healthy` otherwise

### Heartbeat behavior

The worker exposes `LastHeartbeat`, and the health check compares it against a hardcoded threshold of 2 minutes.

If the last heartbeat is older than that, readiness becomes `Unhealthy`.

### Stale-running behavior

The check currently flags a task as stale when:

- `Status == Running`
- and `StartedAt` is older than 15 minutes or missing

This is used only for health reporting. It does not itself recover the task.

### Important implementation note

The worker’s actual stale-task recovery timeout is configurable through `BackgroundTaskSettings.RunningTaskTimeoutMinutes`, but the health check currently uses its own hardcoded 15-minute threshold.

That means the health signal can drift out of sync with real worker recovery behavior if a project changes the worker settings.

## Stuck Emails Health Check

`StuckEmailsHealthCheck` monitors queued emails that appear not to be progressing.

### What it measures

It queries `EmailLogs` for records where:

- `Status == Queued`
- `ScheduledDate < now - 30 minutes`

### Status behavior

- `Degraded` if one or more queued emails are older than the threshold
- `Healthy` if none are stale
- `Unhealthy` if the query throws

### Data returned

The check currently includes:

- `stuckCount`
- `thresholdMinutes`

### Important behavior note

The check uses `ScheduledDate`, not `CreateDate`.

That matters because requeued emails update `ScheduledDate`, so an old email that was just manually requeued does not immediately show up as stuck.

## Aggregation Semantics

ASP.NET Core health checks aggregate the final endpoint status from the underlying checks.

Operationally:

- if any executed check is `Unhealthy`, the overall endpoint status becomes `Unhealthy`
- if no executed check is `Unhealthy` but at least one is `Degraded`, the overall endpoint status becomes `Degraded`
- otherwise the endpoint is `Healthy`

That means `/health/ready` should be treated as the true deployment readiness surface, not just a debug endpoint.

## Operational Usage

### Use `/health` for liveness

Use `/health` when you only need to know whether the process is up and able to answer HTTP requests.

This is the safer endpoint for:

- basic uptime checks
- restart policies
- simple process liveness probes

This endpoint is the one you can expose most safely if you need anonymous health visibility.

### Use `/health/ready` for readiness

Use `/health/ready` when you want to know whether the application is operational enough to receive traffic.

This is the better endpoint for:

- Kubernetes readiness probes
- deployment gates
- dashboards and monitoring alerts

If the probe originates off-host, make sure it sends the configured readiness header.

## Current Strengths

The current health-check implementation is strong in a few important ways:

- it separates liveness from readiness
- it keeps detailed readiness data off the public path by default
- it returns structured JSON
- it includes useful diagnostic data in readiness responses
- it checks both local worker heartbeat and persisted queue state
- it catches stuck queued emails explicitly

For a starter codebase, that is a solid operational baseline.

## Current Limitations

The most important limitations in the current implementation are:

### 1. Background stale-task threshold is duplicated

`BackgroundWorkerHealthCheck` uses a hardcoded 15-minute stale-running threshold instead of reading the same setting the worker uses for recovery.

This can create false alarms if worker settings are tuned for longer-running jobs.

### 2. Background health check is relatively expensive

The background-worker readiness check currently runs four separate count queries against `BackgroundTasks` on every probe.

That is acceptable for a starter or moderate traffic, but it is heavier than a minimal health path.

### 3. Database check is connectivity-only

`DatabaseHealthCheck` does not tell you whether the schema is correct or whether important queries are succeeding quickly.

### 4. Email health check is email-specific

The readiness model includes explicit email backlog monitoring, but there are no equivalent stuck-item checks yet for:

- webhook deliveries
- import tasks
- other future background job types

### 5. Readiness protection is shared-secret based

The current readiness protection is intentionally simple:

- local-request allow
- shared secret for non-local access

That is practical for a starter, but teams with stricter requirements may prefer:

- network allowlists
- reverse-proxy enforcement
- authenticated ops endpoints
- platform-native private health probes

## Recommended Monitoring Approach

For a new project using this starter:

1. use `/health` for liveness probes
2. use `/health/ready` for readiness probes
3. configure `HealthEndpoints:ReadySharedSecret` in non-local environments
4. if your orchestrator probes from another host, make sure it sends the configured readiness header
5. monitor `Degraded` responses, not just `Unhealthy`
6. inspect the `checks[].data` payloads when diagnosing worker or email issues
7. keep probe frequency reasonable, especially because readiness currently performs DB work
8. if you tune background worker timeouts, remember that health-check stale-task thresholds are not automatically kept in sync

## Future Improvements

If a project needs a more mature operational surface later, the next logical improvements would be:

- align `BackgroundWorkerHealthCheck` with `BackgroundTaskSettings`
- reduce DB work per readiness probe
- add stuck-item checks for other task types
- add latency or timeout-oriented DB checks
- add environment-specific readiness behavior if some dependencies are optional

## Summary

The health-check system is already useful and production-shaped for a starter project. It gives clear liveness and readiness endpoints, structured results, and meaningful operational checks around the database, background worker, and queued emails.

The main caveats are that the background-worker readiness check currently duplicates one timeout threshold, does more database work than an ideal lightweight probe, and protects non-local readiness access with a simple shared-secret model. Those are worth knowing, but they do not prevent the current health-check system from being a strong baseline for new projects built on DevCoreApp.
