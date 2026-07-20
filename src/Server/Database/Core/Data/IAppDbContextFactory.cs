namespace DevInstance.DevCoreApp.Server.Database.Core.Data;

/// <summary>
/// Provider-agnostic seam that mints a fresh <see cref="ApplicationDbContext"/> bound to the
/// current scope's <see cref="IOperationContext"/> (so audit logging and organization query
/// filters stay correct). Each provider implements this by constructing its concrete context
/// subtype from built-once options plus the scoped operation context.
/// </summary>
public interface IAppDbContextFactory
{
    ApplicationDbContext CreateDbContext();
}
