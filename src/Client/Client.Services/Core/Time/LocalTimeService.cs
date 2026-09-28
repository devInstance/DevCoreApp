using DevInstance.DevCoreApp.Shared.Utils.Core;

namespace DevInstance.DevCoreApp.Client.Services.Core.Time;

/// <summary>
/// The one place UTC becomes local time (docs/WasmMigrationPlan.md, D6). Everything from the API is
/// UTC; pages display through this service (or the <c>LocalTime</c> component) and convert user
/// input back with <see cref="ToUtc"/> in the client service, never in the page.
/// </summary>
public interface ILocalTimeService
{
    /// <summary>The user's profile time zone when set and known, else the browser's.</summary>
    TimeZoneInfo TimeZone { get; }

    /// <summary>Raised when <see cref="TimeZone"/> changes (sign-in, profile edit).</summary>
    event Action? Changed;

    /// <summary>Sets the zone from a profile <c>TimeZoneId</c>; null or unknown falls back to the browser zone.</summary>
    void SetTimeZone(string? timeZoneId);

    /// <summary>UTC (or Unspecified, taken as UTC) → wall-clock time in <see cref="TimeZone"/>.</summary>
    DateTime ToLocal(DateTime utc);

    /// <summary>Wall-clock time in <see cref="TimeZone"/> → UTC.</summary>
    DateTime ToUtc(DateTime local);

    /// <summary>Nullable <see cref="ToUtc(DateTime)"/>, for optional date filters.</summary>
    DateTime? ToUtc(DateTime? local) => local.HasValue ? ToUtc(local.Value) : null;

    /// <summary>Formats a UTC value in <see cref="TimeZone"/>; <paramref name="empty"/> for null.</summary>
    string Format(DateTime? utc, string format = "g", string empty = "");
}

public sealed class LocalTimeService : ILocalTimeService
{
    public TimeZoneInfo TimeZone { get; private set; } = TimeZoneInfo.Local;

    public event Action? Changed;

    public void SetTimeZone(string? timeZoneId)
    {
        // In WebAssembly TimeZoneInfo.Local is the browser's zone.
        var zone = DateTimeExtensions.ResolveTimeZone(timeZoneId) ?? TimeZoneInfo.Local;
        if (zone.Id != TimeZone.Id)
        {
            TimeZone = zone;
            Changed?.Invoke();
        }
    }

    public DateTime ToLocal(DateTime utc)
    {
        var asUtc = utc.Kind switch
        {
            DateTimeKind.Utc => utc,
            DateTimeKind.Local => utc.ToUniversalTime(),
            _ => DateTime.SpecifyKind(utc, DateTimeKind.Utc)
        };
        // Unspecified kind: the result is wall-clock time in TimeZone, which is not necessarily
        // the browser's zone, so it must not claim to be DateTimeKind.Local.
        return DateTime.SpecifyKind(TimeZoneInfo.ConvertTimeFromUtc(asUtc, TimeZone), DateTimeKind.Unspecified);
    }

    public DateTime ToUtc(DateTime local)
    {
        if (local.Kind == DateTimeKind.Utc)
        {
            return local;
        }

        var wallClock = DateTime.SpecifyKind(local, DateTimeKind.Unspecified);
        if (TimeZone.IsInvalidTime(wallClock))
        {
            // Inside a DST spring-forward gap: that clock time never happens. Move past the gap.
            wallClock = wallClock.AddHours(1);
        }

        return TimeZoneInfo.ConvertTimeToUtc(wallClock, TimeZone);
    }

    public string Format(DateTime? utc, string format = "g", string empty = "") =>
        utc.HasValue ? ToLocal(utc.Value).ToString(format) : empty;
}
