# Per-Operation Unit of Work (Blazor Server concurrency safety)

**Rule of thumb:** a service method gets its own short-lived database context. Open one with
`await using var repo = RepositoryFactory.Create();` at the top of the method, run all its
queries through that `repo`, and let `using` dispose it. Never share one context across
concurrent work.

## Why this exists

Blazor **interactive Server** runs every component in a circuit inside **one DI scope**, so a
scoped `DbContext` (the default `AddDbContext` lifetime) is shared by *all* components on the
page. Blazor initializes components cooperatively: when one component `await`s a DB round-trip,
another component's `OnInitializedAsync` continues and can start a **second** operation on that
**same** context. EF Core / Npgsql then throw:

- `System.InvalidOperationException: A second operation was started on this context instance...`
- `System.InvalidOperationException: Connection is not open`

The classic trigger is a shell component (theme/settings/notifications in the layout) whose data
load overlaps a page's data load. It is intermittent — it only fires when two operations land on
the connection at the same instant.

Two things this is **not** (don't be misled):

- **Not** prerender-vs-SignalR. Prerendering and the interactive circuit get *separate* DI scopes
  (separate contexts), so they can't collide with each other. Disabling prerender does **not** fix
  this.
- **Not** the `Host.BeginServiceCall().DispatchCall(...).ExecuteAsync()` fan-out. `ExecuteAsync`
  runs its queued calls **sequentially**, so calls inside one page don't overlap each other. The
  overlap is *across* components sharing the scoped context.

## The fix

Give **each logical operation its own short-lived context**, created from a factory and disposed
when the operation ends. Different operations never share a context (concurrency-safe); every query
inside one operation shares one context (multi-step writes stay one unit of work / one transaction).

### Infrastructure (Database layer — add once per template)

`IAppDbContextFactory` (Core) — provider-agnostic seam that mints a context bound to the scoped
`IOperationContext` (so audit logging + org query filters stay correct):

```csharp
public interface IAppDbContextFactory
{
    ApplicationDbContext CreateDbContext();
}
```

`PostgresAppDbContextFactory` (Postgres) — built-once options + the live scoped `IOperationContext`:

```csharp
public sealed class PostgresAppDbContextFactory : IAppDbContextFactory
{
    private readonly DbContextOptions<PostgresApplicationDbContext> _options;
    private readonly IOperationContext _operationContext;

    public PostgresAppDbContextFactory(DbContextOptions<PostgresApplicationDbContext> options,
                                       IOperationContext operationContext)
    { _options = options; _operationContext = operationContext; }

    public ApplicationDbContext CreateDbContext()
        => new PostgresApplicationDbContext(_options, _operationContext);
}
```

`IQueryRepositoryFactory` + `QueryRepositoryFactory` (Core) — hands back a repository that **owns**
its context:

```csharp
public interface IQueryRepositoryFactory { IQueryRepository Create(); }

public sealed class QueryRepositoryFactory : IQueryRepositoryFactory
{
    private readonly IScopeManager _logManager;
    private readonly ITimeProvider _timeProvider;
    private readonly IAppDbContextFactory _contextFactory;
    // ...ctor...
    public IQueryRepository Create()
        => new CoreQueryRepository(_logManager, _timeProvider, _contextFactory.CreateDbContext(), ownsContext: true);
}
```

`IQueryRepository : IAsyncDisposable`; `CoreQueryRepository` becomes concrete, takes an
`ownsContext` flag, and disposes the context only when it owns it:

```csharp
public async ValueTask DisposeAsync()
{
    if (_ownsContext) await DB.DisposeAsync();
}
```

DI registration (`ConfigurePostgresDatabase`) — keep the scoped repository **and** add the factory:

```csharp
// Scoped repository over the ambient DI-scoped context — for code that already opens its own
// scope per operation (background workers, import handlers, HttpOperationContext, seeders, Identity).
services.AddScoped<IQueryRepository, PostgresQueryRepository>();

// Per-operation unit-of-work infrastructure (Blazor Server concurrency safety).
var unitOfWorkOptions = new DbContextOptionsBuilder<PostgresApplicationDbContext>()
    .UseNpgsql(connectionString, b => b.MigrationsAssembly("DevInstance.DevCoreApp.Server.Database.Postgres"))
    .Options;
services.AddScoped<IAppDbContextFactory>(sp =>
    new PostgresAppDbContextFactory(unitOfWorkOptions, sp.GetRequiredService<IOperationContext>()));
services.AddScoped<IQueryRepositoryFactory, QueryRepositoryFactory>();
```

`BaseService` exposes the factory instead of a shared repository:

```csharp
public IQueryRepositoryFactory RepositoryFactory { get; }
```

## Writing a service (the pattern)

```csharp
public async Task<ServiceActionResult<ContactItem>> AddAsync(ContactItem item)
{
    using var l = log.TraceScope();
    await using var repo = RepositoryFactory.Create();      // one unit of work

    var query = repo.GetContactQuery(AuthorizationContext.CurrentProfile);
    var record = query.CreateNew();
    record.ToRecord(item);
    await query.AddAsync(record);
    return await GetAsync(record.PublicId);                 // separate method → its own repo
}
```

- **One `repo` per public method.** Every query in that method uses the same `repo`, so
  read → create → `SaveChangesAsync` remains atomic.
- **Private helpers that touch data take `IQueryRepository repo` as a parameter** and the caller
  passes its own `repo` — do **not** open a second `repo` inside a helper, or the operation splits
  across two contexts/transactions:
  ```csharp
  private async Task<Guid?> ResolveCompanyIdAsync(IQueryRepository repo, string? id, string? name) { ... }
  ```
- **Never inject `ApplicationDbContext` into a Blazor-facing service.** If you need a DbSet a query
  class doesn't expose, add a thin `QueryXxx()` / `AddXxx()` to `IQueryRepository` /
  `CoreQueryRepository` and call it through `repo`.

## When the ambient scoped repository is still fine

Code that already runs **one operation per DI scope** does not have the concurrency problem and may
keep injecting the scoped `IQueryRepository` (or, where unavoidable, `ApplicationDbContext`):

- Background job handlers / hosted services (they create a scope per job).
- Data seeders that run at startup.
- Claims transformation, `IOperationContext` resolvers, and other per-request auth-pipeline code.

Everything reachable from a **Blazor component** must use `RepositoryFactory.Create()`.

## Regression guard

`UnitOfWorkConcurrencyTests` fires 32 overlapping operations through the real
`QueryRepositoryFactory` and asserts none trip EF's concurrency detector. Keep it green — if
someone regresses the factory into handing back a shared context, that test starts failing.
