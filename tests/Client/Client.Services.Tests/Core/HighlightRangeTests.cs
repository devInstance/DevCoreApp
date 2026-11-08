using System.Text.Json;
using DevInstance.DevCoreApp.Shared.Model.Core.Common;
using DevInstance.WebServiceToolkit.Common.Model;
using Xunit;

namespace DevInstance.DevCoreApp.Client.Services.Tests.Core;

public class HighlightRangeTests
{
    private static string Render(string? text, params HighlightRange[] ranges) =>
        string.Concat(HighlightRange.Segments(text, ranges).Select(s => s.IsMatch ? $"[{s.Text}]" : s.Text));

    [Fact]
    public void marks_each_range()
    {
        Assert.Equal("[Jo]hn [Jo]nes", Render("John Jones", new(0, 2), new(5, 2)));
    }

    [Fact]
    public void no_ranges_gives_back_the_text()
    {
        Assert.Equal("Acme", Render("Acme"));
        Assert.Empty(HighlightRange.Segments(null, new[] { new HighlightRange(0, 2) }));
    }

    [Fact]
    public void ranges_are_sorted_and_overlapping_or_touching_ones_merged()
    {
        Assert.Equal("[abcde]f", Render("abcdef", new(3, 2), new(0, 2), new(1, 3)));
        Assert.Equal("[abcd]ef", Render("abcdef", new(0, 2), new(2, 2)));
    }

    [Fact]
    public void ranges_are_clipped_to_a_truncated_preview()
    {
        // The server's ranges are for the whole body; the page shows the first 5 characters.
        Assert.Equal("ab[cde]", Render("abcde", new(2, 10), new(20, 3)));
    }

    [Fact]
    public void highlights_survive_the_web_json_round_trip_with_property_names_intact()
    {
        var list = PagedList.Create(new[] { "x" }, search: "Jo");
        list.Highlights = new()
        {
            ["c1"] = new() { ["FullName"] = new[] { new HighlightRange(0, 2) } },
        };

        var json = JsonSerializer.Serialize(list, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        var back = JsonSerializer.Deserialize<PagedList<string>>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;

        // Dictionary keys are not camel-cased, so nameof(Item.Prop) on the client finds them.
        Assert.Contains("\"FullName\"", json);
        var range = Assert.Single(back.HighlightsFor("c1", "FullName"));
        Assert.Equal((0, 2), (range.Start, range.Length));
    }
}
