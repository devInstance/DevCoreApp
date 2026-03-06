namespace DevInstance.DevCoreApp.Shared.Model.ApiKeys;

public class ApiKeyCreateResult
{
    public ApiKeyItem Key { get; set; } = default!;

    /// <summary>
    /// The full plain-text API key. Only available at creation time — never stored or returned again.
    /// </summary>
    public string PlainTextKey { get; set; } = string.Empty;
}
