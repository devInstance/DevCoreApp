using DevInstance.DevCoreApp.Server.Database.Core.Models;
using DevInstance.DevCoreApp.Shared.Model.FeatureFlags;

namespace DevInstance.DevCoreApp.Server.Database.Core.Data.Decorators;

public static class FeatureFlagDecorators
{
    public static FeatureFlagItem ToView(this FeatureFlag flag)
    {
        return new FeatureFlagItem
        {
            Id = flag.PublicId,
            Name = flag.Name,
            Description = flag.Description,
            IsEnabled = flag.IsEnabled,
            OrganizationId = flag.Organization?.PublicId,
            OrganizationName = flag.Organization?.Name,
            RolloutPercentage = flag.RolloutPercentage,
            AllowedUsers = flag.AllowedUsers,
            CreateDate = flag.CreateDate,
            UpdateDate = flag.UpdateDate
        };
    }

    public static FeatureFlag ToRecord(this FeatureFlag flag, FeatureFlagItem item)
    {
        flag.Name = item.Name;
        flag.Description = item.Description;
        flag.IsEnabled = item.IsEnabled;
        flag.RolloutPercentage = item.RolloutPercentage;
        flag.AllowedUsers = item.AllowedUsers;
        return flag;
    }
}
