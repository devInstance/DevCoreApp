using DevInstance.DevCoreApp.Server.Database.Core.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;

namespace DevInstance.DevCoreApp.Server.Database.Postgres;

/// <summary>
/// Factory used by EF Core tools (dotnet ef migrations, dotnet ef database update).
/// Reads the connection string from the WebService appsettings.json.
///
/// Usage:
///   dotnet ef migrations add MyMigration
///     --project src/Server/Database/Postgres
///     --startup-project src/Server/Admin/WebService
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<PostgresApplicationDbContext>
{
    public PostgresApplicationDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(FindWebServiceDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var connectionString = configuration.GetConnectionString("PostgresConnection");

        var optionsBuilder = new DbContextOptionsBuilder<PostgresApplicationDbContext>();
        optionsBuilder.UseNpgsql(
            connectionString,
            b => b.MigrationsAssembly("DevInstance.DevCoreApp.Server.Database.Postgres"));

        return new PostgresApplicationDbContext(optionsBuilder.Options, new NullOperationContext());
    }

    /// <summary>
    /// Locates the WebService project directory by walking up from the current directory
    /// to find the solution root, then resolving the known relative path.
    /// Falls back to the current directory (works when --startup-project is specified).
    /// </summary>
    private static string FindWebServiceDirectory()
    {
        // When --startup-project is specified, EF tools set the content root
        // to that project's directory. Check if appsettings.json exists here first.
        var currentDir = Directory.GetCurrentDirectory();
        if (File.Exists(Path.Combine(currentDir, "appsettings.json")))
        {
            return currentDir;
        }

        // Walk up to find the solution root (contains the .sln file),
        // then resolve the WebService project path.
        var dir = new DirectoryInfo(currentDir);
        while (dir != null)
        {
            if (Directory.GetFiles(dir.FullName, "*.sln").Length > 0)
            {
                var webServiceDir = Path.Combine(dir.FullName, "src", "Server", "Admin", "WebService");
                if (Directory.Exists(webServiceDir))
                {
                    return webServiceDir;
                }
            }
            dir = dir.Parent;
        }

        return currentDir;
    }

    private class NullOperationContext : IOperationContext
    {
        public Guid? UserId => null;
        public Guid? PrimaryOrganizationId => null;
        public IReadOnlySet<Guid> VisibleOrganizationIds => new HashSet<Guid>();
        public string? IpAddress => null;
        public string? CorrelationId => null;
    }
}
