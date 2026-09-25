using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevInstance.DevCoreApp.Shared.Utils.Core;

public static class DateTimeExtensions
{
    public static DateTime UTCKind(this DateTime dt)
    {
        return DateTime.SpecifyKind(dt, DateTimeKind.Utc);
    }

    /// <summary>
    /// Resolves a stored TimeZoneId to a TimeZoneInfo, returning null for an unset or
    /// unrecognized id rather than throwing. An id that exists on one OS and not another is a
    /// normal condition, not an error, so every caller wants the same lenient behavior —
    /// this is the one place it lives.
    /// </summary>
    public static TimeZoneInfo? ResolveTimeZone(string? timeZoneId)
    {
        if (string.IsNullOrEmpty(timeZoneId)) return null;
        try { return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId); }
        catch { return null; }
    }

    /// <summary>
    /// Returns "now" expressed in the user's local clock. Use this from Blazor SSR
    /// pages/components when seeding ViewModels — DateTime.Now reflects the server's clock
    /// (UTC in most cloud deployments), not the user's. When tz is null, returns the UTC
    /// clock unshifted (best effort when the user has no TimeZoneId set).
    /// </summary>
    public static DateTime NowInZone(TimeZoneInfo? tz)
    {
        return tz == null
            ? DateTime.UtcNow
            : TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
    }

    public static DateTime ToLocal(this DateTime utcDate, TimeZoneInfo? tz)
    {
        if (tz == null) return utcDate;
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utcDate, DateTimeKind.Utc), tz);
    }

    public static DateTime? ToLocal(this DateTime? utcDate, TimeZoneInfo? tz)
    {
        if (!utcDate.HasValue || tz == null) return utcDate;
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utcDate.Value, DateTimeKind.Utc), tz);
    }

    public static DateTime ToUtc(this DateTime localDate, TimeZoneInfo? tz)
    {
        if (tz == null) return DateTime.SpecifyKind(localDate, DateTimeKind.Utc);
        return TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(localDate, DateTimeKind.Unspecified), tz);
    }

    public static DateTime? ToUtc(this DateTime? localDate, TimeZoneInfo? tz)
    {
        if (!localDate.HasValue) return localDate;
        if (tz == null) return DateTime.SpecifyKind(localDate.Value, DateTimeKind.Utc);
        return TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(localDate.Value, DateTimeKind.Unspecified), tz);
    }
}
