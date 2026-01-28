using DevInstance.DevCoreApp.Server.Database.Core.Models.Base;
using System;

namespace DevInstance.DevCoreApp.Server.Database.Core.Models;

/// <summary>
/// Stores user's grid preferences (column visibility, order, sorting, page size)
/// for a specific grid identified by GridName.
/// </summary>
public class GridProfile : DatabaseObject
{
    /// <summary>
    /// Reference to the user who owns this profile
    /// </summary>
    public Guid UserProfileId { get; set; }

    /// <summary>
    /// Name/identifier of the grid (e.g., "admin/users", "admin/orders")
    /// </summary>
    public string GridName { get; set; } = string.Empty;

    /// <summary>
    /// Profile name for supporting multiple profiles per grid (e.g., "Default", "Compact")
    /// </summary>
    public string ProfileName { get; set; } = "Default";

    /// <summary>
    /// JSON serialized column states (visibility, order)
    /// </summary>
    public string ColumnsJson { get; set; } = string.Empty;

    /// <summary>
    /// Number of items per page
    /// </summary>
    public int PageSize { get; set; } = 10;

    /// <summary>
    /// Current sort field
    /// </summary>
    public string? SortField { get; set; }

    /// <summary>
    /// Sort direction (true = ascending, false = descending)
    /// </summary>
    public bool IsAsc { get; set; } = true;

    /// <summary>
    /// If true, profile is visible to all users but can only be edited by Owner, Admin, or Manager.
    /// If false, profile belongs to UserProfileId and is only visible to that user.
    /// </summary>
    public bool IsGlobal { get; set; } = false;

    /// <summary>
    /// Navigation property to UserProfile
    /// </summary>
    public UserProfile? UserProfile { get; set; }
}
