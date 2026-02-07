# Database Providers

The database layer is divided into "Core" and "Provider". "Core" contains all the interfaces and common logic, and is independent of any specific database engine. "Provider" should implement the interfaces and add the database-specific code, including migrations.

## Core

Core is structured as follows:

- **Data:** Contains logic and interfaces needed to execute queries. The IQueryRepository interfaces define queries. Each query is meant to encapsulate actions that can be performed against a specific object type. For example, the "Employee" table can have Select, Add, Update, and Delete methods. They can all be declared as IEmployeeQuery. Additionally, IEmployee query can inherit IModelQuery or other common interfaces from Base.
- **Models:** This namespace is for database model objects.
- **ApplicationDbContext.cs:** Database context. It is an abstract class and should be implemented by a specific context in the provider rather than used directly.
- **ConfigurationExtensions.cs:** Generic methods that encapsulate configuring code (usually called in Startup.cs).

## Provider

The Provider implements database engine-specific logic. There are two providers in this project as of now: Postgres and SqlServer. The structure is similar to the Core. Every provider should:

1. Override or implement the database context;
2. Implement interfaces in the Queries namespace;
3. Provide migrations (see Migrations section below);
4. Provide configuration extensions.

## Configuration

The provider configuration takes place in the server project. Depending on the project itself, developers may choose to support a single or multiple providers. For multiple configurations, Startup.cs already has the code needed to support provider selection. In this case, appsettings.json should have the following configuration:

`
    "Database": {
        "Provider": <Provider here "Postgres" or “SqlServer”>
    },

    "ConnectionStrings": {
        "PostgresConnection": "…",
        "SqlServerConnection": "…"
    },
`

In case only one provider is supported, the configuration can be simplified:

1. Remove all references to the provider from the WebService project except for the one that will be used;
2. Modify appsettings.json to support only one connection string;
3. Simplify configuring the database in Startup.cs:

`
//Configuring Postgres
private void ConfigureDatabase(IServiceCollection services)
{
    services.ConfigurePostgresDatabase(Configuration);
    services.AddDatabaseDeveloperPageExceptionFilter();
    services.ConfigurePostgresIdentityContext();
}
`

## Migrations

Migrations are owned by the provider project. To create a migration, navigate to the WebService project directory and run:

**Working Directory:** `src\Server\WebService\DevCoreApp.Admin.WebService`

**Command:**
```
dotnet ef migrations add <Migration Name> --project ..\Database\<Provider>
```

**Example:**
```
dotnet ef migrations add CreateIdentitySchema --project ..\Database\Postgres
```

To apply a migration to the local development database, use:

```
dotnet ef database update
```

Production deployment can be done by running a script. The production script is usually stored in the deployment/sql/<provider> folder and should have the full name of the migration. Generate the script with:

```
dotnet ef migrations script --project ..\Database\<provider> > ..\..\..\deployment\sql\<provider>\<migration>.sql
```

**Example:**
```
dotnet ef migrations script --project ..\Database\Postgres > ..\..\..\deployment\sql\postgres\20210728041133_CreateIdentitySchema.sql
```

If you encounter the "Command dotnet ef not found" error, please install the EF tool:

```
dotnet tool install --global dotnet-ef
```

