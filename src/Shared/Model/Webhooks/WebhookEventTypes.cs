namespace DevInstance.DevCoreApp.Shared.Model.Webhooks;

public static class WebhookEventTypes
{
    public const string UserCreated = "User.Created";
    public const string UserUpdated = "User.Updated";
    public const string UserDeleted = "User.Deleted";
    public const string OrganizationCreated = "Organization.Created";
    public const string OrganizationUpdated = "Organization.Updated";

    public static readonly string[] All = new[]
    {
        UserCreated,
        UserUpdated,
        UserDeleted,
        OrganizationCreated,
        OrganizationUpdated
    };
}
