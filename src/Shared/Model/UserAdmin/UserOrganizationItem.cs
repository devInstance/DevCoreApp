using DevInstance.DevCoreApp.Shared.Model.Common;

namespace DevInstance.DevCoreApp.Shared.Model.UserAdmin;

public class UserOrganizationItem
{
    public string OrganizationId { get; set; } = string.Empty;
    public string OrganizationName { get; set; } = string.Empty;
    public string OrganizationPath { get; set; } = string.Empty;
    public OrganizationAccessScope Scope { get; set; }
    public bool IsPrimary { get; set; }
}
