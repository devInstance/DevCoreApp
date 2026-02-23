using System.Threading;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Server.Database.Core.Data;

/// <summary>
/// Implementations are executed after database migrations on application startup.
/// Register each seeder in DI as IDataSeeder. The startup pipeline resolves all
/// registered instances and calls SeedAsync on each.
/// </summary>
public interface IDataSeeder
{
    /// <summary>
    /// The order in which this seeder runs relative to others. Lower values run first.
    /// </summary>
    int Order { get; }

    /// <summary>
    /// Performs idempotent seed operations. Implementations must check for existing
    /// data before inserting to support repeated startup calls.
    /// </summary>
    Task SeedAsync(CancellationToken cancellationToken = default);
}
