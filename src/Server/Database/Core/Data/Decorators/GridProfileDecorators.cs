using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.DevCoreApp.Shared.Model;
using System.Collections.Generic;
using System.Text.Json;

namespace DevInstance.DevCoreApp.Server.Database.Core.Data.Decorators;

public static class GridProfileDecorators
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static GridProfileItem ToView(this GridProfile profile)
    {
        var columns = new List<GridColumnState>();

        if (!string.IsNullOrEmpty(profile.ColumnsJson))
        {
            try
            {
                columns = JsonSerializer.Deserialize<List<GridColumnState>>(profile.ColumnsJson, JsonOptions)
                          ?? new List<GridColumnState>();
            }
            catch
            {
                columns = new List<GridColumnState>();
            }
        }

        return new GridProfileItem
        {
            Id = profile.PublicId,
            GridName = profile.GridName,
            ProfileName = profile.ProfileName,
            Columns = columns,
            PageSize = profile.PageSize,
            SortField = profile.SortField,
            IsAsc = profile.IsAsc,
            IsGlobal = profile.IsGlobal,
            CreateDate = profile.CreateDate,
            UpdateDate = profile.UpdateDate
        };
    }

    public static GridProfile ToRecord(this GridProfile profile, GridProfileItem item)
    {
        profile.GridName = item.GridName;
        profile.ProfileName = item.ProfileName;
        profile.ColumnsJson = JsonSerializer.Serialize(item.Columns, JsonOptions);
        profile.PageSize = item.PageSize;
        profile.SortField = item.SortField;
        profile.IsAsc = item.IsAsc;
        profile.IsGlobal = item.IsGlobal;

        return profile;
    }
}
