using System.Collections.Generic;

namespace DevInstance.DevCoreApp.Shared.Model.Core.UserAdmin;

/// <summary>
/// Human-readable labels for the <c>UserStatus</c> values that cross the API as strings.
/// <para>
/// The stored value stays the enum name (<c>LIVE</c>, <c>INITIATED</c>) — it is the machine value,
/// and comparisons, serialization and saved grid profiles depend on it being stable. Display goes
/// through <see cref="For"/> so the shouty uppercase never reaches a screen.
/// </para>
/// <para>
/// <b>This is the single localization seam.</b> When resources arrive, <see cref="For"/> becomes a
/// lookup on <see cref="ResourceKey"/> against an <c>IStringLocalizer</c>, falling back to the
/// defaults below; nothing else has to change, because no caller hardcodes a label. The keys are
/// already in the conventional <c>UserStatus_Live</c> shape.
/// </para>
/// <para>
/// Lives in <c>Shared.Model</c> rather than beside the enum so the WASM client can render the same
/// labels — it cannot reference the Database project where <c>UserStatus</c> is declared.
/// </para>
/// </summary>
public static class UserStatusLabels
{
    public const string Unknown = "UNKNOWN";
    public const string Initiated = "INITIATED";
    public const string Live = "LIVE";
    public const string Suspended = "SUSPENDED";

    private static readonly Dictionary<string, string> Defaults = new(System.StringComparer.OrdinalIgnoreCase)
    {
        [Unknown] = "Unknown",
        // "Invited" rather than "Initiated": the account exists but its owner has not completed the
        // invitation, which is what an administrator actually wants to read off the list.
        [Initiated] = "Invited",
        [Live] = "Active",
        [Suspended] = "Suspended"
    };

    /// <summary>
    /// The display label for a status value. An unrecognised value is returned unchanged rather
    /// than blanked — losing it would hide exactly the case worth noticing.
    /// </summary>
    public static string For(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return Defaults[Unknown];
        }

        return Defaults.TryGetValue(status, out var label) ? label : status;
    }

    /// <summary>
    /// The resource key a future <c>IStringLocalizer</c> would look up, e.g. <c>UserStatus_Live</c>.
    /// </summary>
    public static string ResourceKey(string? status)
    {
        var value = string.IsNullOrWhiteSpace(status) ? Unknown : status;
        return "UserStatus_" + char.ToUpperInvariant(value[0]) + value.Substring(1).ToLowerInvariant();
    }
}
