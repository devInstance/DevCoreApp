# Overview

The database layer has been divided into "Core" and "Provider". "Core" contains all the interface and the common logic and is independent of specific database engine. "Provider" should implement the interfaces and add the database specific code including migrations.

# Core

Core is structured in the following way:
- **Data:** contains logic and interface needed to realize queries. IQueryRepository interfaces define queries. Every query spouse to encapsulate actions that an be performed against the specific object type. For instance, “Emplyee” table can have Select, Add, Update, Delete methods. They all can be declared as IEmplyeeQuery. Additionally, IEmployee query can inherit IModelQuery or other common interfaces from Base.
- **Models:** is the namespace for database model objects
- **ApplicationDbContext.cs:** database context. It is an abstract class and should be implemented by a specific context in the provider rather than used directly 
- **ConfigurationExtensions.cs:** generic methods that encapsulates configuring code (usually called in Startup.cs)

# Provider

Provider implements a database engine specific logic. There are two providers in this project as of right now: Postgres and SqlServer. The structure is the similar to the code. Every provider should:

1. Override or implement database context;
2. Implement interfaces in the Queries namespace;
3. Provide migrations (see Migrations section below);
4. Provide configuration extensions;

# Configuration

The provider configuration is happening in the server project. Depending on the project itself, developer may choose to support single or multiple providers. For multiple configurations Startup.cs has already the code need to support the provider selection. In this case the appsettings.json should have the following configuration:
`
    "Database": {
        "Provider": <Provider here "Postgres" or “SqlServer”>
    },

    "ConnectionStrings": {
        "PostgresConnection": "…",
        "SqlServerConnection": "…"
    },
`

In case the only one provider is supported the configuration can be simplified:
1. Remove all the references to the provider from WebService project but one which will be used;
2. appsettings.json can be change to support only one connection string
3. In Startup.cs configuring database can be simplified:
`
//Configuring Postgres
private void ConfigureDatabase(IServiceCollection services)
{
    services.ConfigurePostgresDatabase(Configuration);
    services.AddDatabaseDeveloperPageExceptionFilter();
    services.ConfigurePostgresIdentityContext();
}
`
# Migrations

Migrations is own by the provider project. Run the following command in WebService project to create a migration:

`SampleWebApp\src\Server\WebService> dotnet ef migrations add <Migration Name>  --project ..\Database\<Provider>`

Example:

`SampleWebApp\src\Server\WebService> dotnet ef migrations add CreateIdentitySchema --project ..\Database\Postgres`

Applying migration to the local development database is done by:

`dotnet ef database update`

Production deployment can be done by running a script. The production script is usually stored in deployment/sql/<provider> folder and should have a full name of the migration. The script generation is done by:

`dotnet ef migrations script  --project ..\Database\<provider> > ..\..\..\deployment\sql\<provider>\<migration>.sql`

Example:

`dotnet ef migrations script  --project ..\Database\Postgres\ > ..\..\..\deployment\sql\postgres\20210728041133_CreateIdentitySchema.sql`

In case you are getting "Command dotnet ef not found", please install EF tool:

`dotnet tool install --global dotnet-ef`

