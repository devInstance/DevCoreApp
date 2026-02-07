namespace DevInstance.DevCoreApp.Server.Admin.WebService.Authentication;

public static class ApplicationRoles
{
    public const string Owner = "Owner";
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string Employee = "Employee";
    public const string Client = "Client";

    public static readonly string[] All = [Owner, Admin, Manager, Employee, Client];
}
