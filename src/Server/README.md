# DevCoreApp: Server

## Overview

The server side of DevCoreApp is split into multiple projects, each with a clear responsibility. This separation keeps dependencies explicit, improves testability, and allows teams to work on different layers independently.

## Project Structure

```
/src/Server/
├── Admin/
│   ├── Services/          # Business logic, authentication, notifications
│   └── WebService/        # Blazor SSR host, API controllers, SignalR, UI pages
├── Database/
│   ├── Core/              # Entities, queries, decorators, EF configuration
│   ├── Postgres/          # PostgreSQL provider and migrations
│   └── SqlServer/         # SQL Server provider and migrations
├── Email/
│   ├── Processor/         # Email abstractions and interfaces
│   ├── MailKit/           # MailKit-based email implementation
│   ├── Smtp/              # SMTP email implementation
│   └── SendGrid/          # SendGrid email implementation
└── Storage/
    ├── Processor/         # File storage abstractions and interfaces
    ├── Local/             # Local disk storage implementation
    └── S3/                # AWS S3 storage implementation
```

## Architecture

The server follows a layered architecture with strict dependency rules:

```
Pages/Controllers → Services → Queries/Repository → Database
```

### WebService (Admin/WebService)

The entry point of the application. Hosts Blazor SSR pages, API controllers, and SignalR hubs. Pages use `IServiceExecutionHost` as a cascading parameter for all service calls. Controllers use `HandleWebRequestAsync()` for consistent error handling. This project never accesses the database directly — all data access goes through Services.

### Services (Admin/Services)

Contains all business logic. Services inherit from `BaseService`, are annotated with `[BlazorService]` for automatic DI registration, and return `ServiceActionResult<T>`. They access data through query classes obtained from `IQueryRepository`, never through `DbContext` directly.

### Database

The Core project defines entities, EF configuration, query classes, and decorators. Postgres and SqlServer projects contain provider-specific migrations and configuration. Query classes implement `IModelQuery<T,D>` and support pagination, search, and sorting. Decorators provide `ToView()` and `ToRecord()` extension methods for mapping between entities and ViewModels.

### Email

Provider-based email sending. The Processor project defines the abstractions; MailKit, Smtp, and SendGrid are swappable implementations. Emails are queued via `IBackgroundWorker` and sent asynchronously.

### Storage

Provider-based file storage. The Processor project defines the abstractions; Local and S3 are swappable implementations. File metadata is stored in the `FileRecords` table; physical files are managed by the active provider.

## Key Principles

- **Services own business logic** — pages and controllers are thin, services are where decisions happen.
- **Query classes own data access** — services never call `DbContext` directly.
- **Decorators own mapping** — `ToView()` converts entities to ViewModels, `ToRecord()` maps DTOs back onto entities.
- **Providers are swappable** — database, email, and storage providers can be changed without touching business logic.
- **Organization-scoped data** — business tables include `OrganizationId` and EF global query filters enforce data isolation automatically.
