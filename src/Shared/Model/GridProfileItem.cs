using DevInstance.WebServiceToolkit.Common.Model;
using System;
using System.Collections.Generic;

namespace DevInstance.DevCoreApp.Shared.Model;

/// <summary>
/// DTO for grid profile settings
/// </summary>
public class GridProfileItem : ModelItem
{
    public string GridName { get; set; } = string.Empty;
    public string ProfileName { get; set; } = "Default";
    public List<GridColumnState> Columns { get; set; } = new();
    public int PageSize { get; set; } = 10;
    public string? SortField { get; set; }
    public bool IsAsc { get; set; } = true;
    /// <summary>
    /// If true, profile is visible to all users but can only be edited by Owner, Admin, or Manager.
    /// If false, profile belongs to the current user and is only visible to that user.
    /// </summary>
    public bool IsGlobal { get; set; } = false;
    public DateTime CreateDate { get; set; }
    public DateTime UpdateDate { get; set; }
}

/// <summary>
/// Represents the state of a single column in the grid
/// </summary>
public class GridColumnState
{
    public string Field { get; set; } = string.Empty;
    public bool IsVisible { get; set; } = true;
    public int Order { get; set; }
}
