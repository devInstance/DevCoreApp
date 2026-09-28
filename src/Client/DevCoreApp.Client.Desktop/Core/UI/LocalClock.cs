using DevInstance.DevCoreApp.Client.Services.Core.Time;

namespace DevInstance.DevCoreApp.Client.Desktop.Core.UI;

/// <summary>
/// Static access to <see cref="ILocalTimeService"/> for places that cannot inject it — mainly the
/// grid column definitions (<c>ValueSelector</c> lambdas in field initializers). Safe in
/// WebAssembly because the process serves exactly one user. Set once in Program.cs.
/// Everything shown to the user goes through here or the <c>LocalTime</c> component: API
/// values are UTC.
/// </summary>
public static class LocalClock
{
    public static ILocalTimeService Service { get; set; } = new LocalTimeService();

    /// <summary>A UTC value in the user's time zone; <paramref name="empty"/> for null.</summary>
    public static string Format(DateTime? utc, string format = "g", string empty = "") =>
        Service.Format(utc, format, empty);
}
