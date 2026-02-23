using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.DevCoreApp.Shared.Model.Organizations;

namespace DevInstance.DevCoreApp.Server.Database.Core.Data.Decorators;

public static class OrganizationDecorators
{
    public static OrganizationItem ToView(this Organization org)
    {
        return new OrganizationItem
        {
            Id = org.PublicId,
            Name = org.Name,
            Code = org.Code,
            ParentId = org.Parent?.PublicId,
            Level = org.Level,
            Path = org.Path,
            Type = org.Type,
            IsActive = org.IsActive,
            Settings = org.Settings,
            SortOrder = org.SortOrder,
            CreateDate = org.CreateDate,
            UpdateDate = org.UpdateDate
        };
    }

    public static Organization ToRecord(this Organization org, OrganizationItem item)
    {
        org.Name = item.Name;
        org.Code = item.Code;
        org.Level = item.Level;
        org.Path = item.Path;
        org.Type = item.Type;
        org.IsActive = item.IsActive;
        org.Settings = item.Settings;
        org.SortOrder = item.SortOrder;

        return org;
    }
}
