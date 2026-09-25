using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DevInstance.DevCoreApp.Shared.Utils.Core.Json;

/// <summary>
/// Enforces the wire rule that every <see cref="DateTime"/> crossing the API is UTC.
/// Registered on both the server (controller JSON options) and the WASM clients, so neither
/// side can emit or accept an ambiguous local time.
///
/// Write: always ISO-8601 round-trip format with a trailing <c>Z</c>. A <c>Local</c> value is
/// converted to UTC; an <c>Unspecified</c> value is taken to already be UTC (EF materializes
/// stored UTC timestamps as Unspecified on some providers).
///
/// Read: a value carrying an offset is converted to UTC; a value without one is taken to be UTC.
/// The result always has <see cref="DateTimeKind.Utc"/>, so <c>ToLocalTime()</c> on the client
/// behaves correctly.
/// </summary>
public sealed class UtcDateTimeJsonConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var text = reader.GetString();
        if (string.IsNullOrEmpty(text))
        {
            throw new JsonException("Expected an ISO-8601 date/time string.");
        }

        if (!DateTime.TryParse(text, CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
                out var value))
        {
            throw new JsonException($"'{text}' is not a valid ISO-8601 date/time.");
        }

        return DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(ToUtc(value).ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'", CultureInfo.InvariantCulture));
    }

    private static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };
}
