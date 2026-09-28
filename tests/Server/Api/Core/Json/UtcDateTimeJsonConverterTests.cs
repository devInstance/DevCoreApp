using System;
using System.Text.Json;
using DevInstance.DevCoreApp.Shared.Utils.Core.Json;
using Xunit;

namespace DevInstance.DevCoreApp.Server.Tests.Core.Json;

public class UtcDateTimeJsonConverterTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        Converters = { new UtcDateTimeJsonConverter() }
    };

    private record Holder(DateTime At, DateTime? Maybe);

    [Fact]
    public void writes_utc_with_z_suffix()
    {
        var at = new DateTime(2026, 9, 25, 14, 30, 0, DateTimeKind.Utc);

        var json = JsonSerializer.Serialize(at, Options);

        Assert.Equal("\"2026-09-25T14:30:00.0000000Z\"", json);
    }

    [Fact]
    public void writes_unspecified_as_utc_unshifted()
    {
        var at = new DateTime(2026, 9, 25, 14, 30, 0, DateTimeKind.Unspecified);

        var json = JsonSerializer.Serialize(at, Options);

        Assert.Equal("\"2026-09-25T14:30:00.0000000Z\"", json);
    }

    [Fact]
    public void writes_local_converted_to_utc()
    {
        var local = new DateTime(2026, 9, 25, 14, 30, 0, DateTimeKind.Local);

        var json = JsonSerializer.Serialize(local, Options);

        var expected = local.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'");
        Assert.Equal($"\"{expected}\"", json);
    }

    [Fact]
    public void reads_offset_value_as_utc()
    {
        var value = JsonSerializer.Deserialize<DateTime>("\"2026-09-25T10:30:00-04:00\"", Options);

        Assert.Equal(DateTimeKind.Utc, value.Kind);
        Assert.Equal(new DateTime(2026, 9, 25, 14, 30, 0, DateTimeKind.Utc), value);
    }

    [Fact]
    public void reads_offsetless_value_as_utc_unshifted()
    {
        var value = JsonSerializer.Deserialize<DateTime>("\"2026-09-25T14:30:00\"", Options);

        Assert.Equal(DateTimeKind.Utc, value.Kind);
        Assert.Equal(new DateTime(2026, 9, 25, 14, 30, 0, DateTimeKind.Utc), value);
    }

    [Fact]
    public void round_trips_nullable_members()
    {
        var original = new Holder(new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc), null);

        var copy = JsonSerializer.Deserialize<Holder>(JsonSerializer.Serialize(original, Options), Options)!;

        Assert.Equal(original.At, copy.At);
        Assert.Equal(DateTimeKind.Utc, copy.At.Kind);
        Assert.Null(copy.Maybe);
    }
}
