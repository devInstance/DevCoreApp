
namespace DevInstance.DevCoreApp.Server.Admin.Services.Background;

public enum BackgroundRequestType
{
    SendEmail,
    ImportData,
    DeliverWebhook
}

public class BackgroundRequestItem
{
    public BackgroundRequestType RequestType { get; set; }

    public object Content { get; set; }

    /// <summary>
    /// Organization the submitting user was acting in, carried onto the persisted
    /// <c>BackgroundTask</c> row.
    /// <para>
    /// <c>BackgroundTask</c> is <c>IOrganizationScoped</c> with a non-nullable column, and
    /// <c>BackgroundWorker.SubmitAsync</c> builds the row inside a fresh DI scope whose operation
    /// context has been reset — so no ambient organization is available at that point and the row
    /// used to default to <see cref="System.Guid.Empty"/>. The organization query filter then
    /// excluded it for every user with resolved organization claims, which is why the Job Dashboard
    /// came up empty. Callers that know their organization must set this.
    /// </para>
    /// <para>
    /// Null is legitimate for genuinely unscoped submissions — account confirmation and
    /// password-reset mail queued from unauthenticated flows — and still writes
    /// <see cref="System.Guid.Empty"/>.
    /// </para>
    /// </summary>
    public Guid? OrganizationId { get; set; }
}
