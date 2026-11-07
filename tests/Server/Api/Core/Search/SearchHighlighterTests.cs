using System;
using System.Linq;
using DevInstance.DevCoreApp.Server.Services.Core.Search;
using DevInstance.DevCoreApp.Shared.Model.Core.Common;
using DevInstance.WebServiceToolkit.Common.Model;
using Xunit;

namespace DevInstance.DevCoreApp.Server.Tests.Core.Search;

public class SearchHighlighterTests
{
    private class Row : IModelItem
    {
        public string Id { get; set; } = default!;
        public string? Name { get; set; }
        public string? Phone { get; set; }
    }

    private static (int, int)[] Spans(HighlightRange[] ranges) => ranges.Select(r => (r.Start, r.Length)).ToArray();

    [Fact]
    public void finds_every_occurrence_of_the_term()
    {
        Assert.Equal(new[] { (0, 2), (9, 2) }, Spans(SearchHighlighter.FindText("an apple an", "an")));
    }

    [Fact]
    public void occurrences_do_not_overlap()
    {
        Assert.Equal(new[] { (0, 2), (2, 2) }, Spans(SearchHighlighter.FindText("aaaaa", "aa")));
    }

    [Fact]
    public void text_matching_ignores_case_by_default_like_the_queries()
    {
        Assert.Equal(new[] { (0, 5) }, Spans(SearchHighlighter.FindText("Plain text", "PLain")));
        Assert.Empty(SearchHighlighter.FindText("John", "john", StringComparison.Ordinal));
    }

    [Fact]
    public void digits_match_across_phone_formatting()
    {
        // "(555) 123-4567": 5551234 spans "555) 123-4", from index 1 to index 10 inclusive.
        Assert.Equal(new[] { (1, 10) }, Spans(SearchHighlighter.FindDigits("(555) 123-4567", "5551234")));
        Assert.Empty(SearchHighlighter.FindDigits("(555) 123-4567", "999"));
    }

    [Fact]
    public void no_search_leaves_the_list_without_highlights()
    {
        var list = PagedList.Create(new[] { new Row { Id = "a", Name = "Acme" } });

        list.WithHighlights(null, h => h.Text(nameof(Row.Name), r => r.Name));
        list.WithHighlights("", h => h.Text(nameof(Row.Name), r => r.Name));

        Assert.Null(list.Highlights);
    }

    [Fact]
    public void highlights_are_keyed_by_item_id_and_field_and_skip_rows_without_a_match()
    {
        var list = PagedList.Create(new[]
        {
            new Row { Id = "a", Name = "Acme", Phone = "(555) 123-4567" },
            new Row { Id = "b", Name = "Globex", Phone = null },
        }, search: "Ac");

        list.WithHighlights("Ac", h => h
            .Text(nameof(Row.Name), r => r.Name)
            .Phone(nameof(Row.Phone), r => r.Phone));

        Assert.NotNull(list.Highlights);
        Assert.Equal(new[] { "a" }, list.Highlights!.Keys);
        Assert.Equal(new[] { (0, 2) }, Spans(list.HighlightsFor("a", nameof(Row.Name))));
        Assert.Null(list.HighlightsFor("a", nameof(Row.Phone)));
        Assert.Null(list.HighlightsFor("b", nameof(Row.Name)));
    }

    [Fact]
    public void phone_fields_match_by_digits_once_the_term_has_three()
    {
        var list = PagedList.Create(new[] { new Row { Id = "a", Phone = "(555) 123-4567" } });

        list.WithHighlights("123-45", h => h.Phone(nameof(Row.Phone), r => r.Phone));
        Assert.Equal(new[] { (6, 6) }, Spans(list.HighlightsFor("a", nameof(Row.Phone))));

        list.WithHighlights("55", h => h.Phone(nameof(Row.Phone), r => r.Phone));
        // Two digits are below the query's guard; only the literal "55" matches.
        Assert.Equal(new[] { (1, 2) }, Spans(list.HighlightsFor("a", nameof(Row.Phone))));
    }
}
