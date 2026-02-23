using System;

namespace DevInstance.DevCoreApp.Server.Database.Core.Models.Base;

/// <summary>
/// Marker interface for entities that are scoped by organization.
/// EF Core global query filters automatically restrict queries to the
/// current user's visible organizations via IOperationContext.
/// </summary>
public interface IOrganizationScoped
{
    Guid OrganizationId { get; set; }
}
