using System;
using System.Collections.Generic;
using System.Linq;

namespace DevInstance.DevCoreApp.Shared.Model.Core.Common;

/// <summary>
/// One search match inside a string field of a list item: <see cref="Start"/> and <see cref="Length"/>
/// are UTF-16 offsets into the field's value exactly as it is on the wire (after the decorator has
/// formatted it). Computed by the server, rendered by the client — see <see cref="PagedList{T}.Highlights"/>.
/// </summary>
public class HighlightRange
{
    public int Start { get; set; }
    public int Length { get; set; }

    public HighlightRange() { }

    public HighlightRange(int start, int length)
    {
        Start = start;
        Length = length;
    }

    /// <summary>
    /// Splits <paramref name="text"/> into consecutive segments, each flagged as matched or not.
    /// Ranges are sorted, clipped to the text (so a truncated preview can reuse the full field's
    /// ranges) and merged where they overlap or touch. Concatenating the segments gives back the text.
    /// </summary>
    public static IReadOnlyList<(string Text, bool IsMatch)> Segments(string? text, IEnumerable<HighlightRange>? ranges)
    {
        var segments = new List<(string, bool)>();
        if (string.IsNullOrEmpty(text))
            return segments;

        var position = 0;
        foreach (var (start, end) in Normalize(text.Length, ranges))
        {
            if (start > position)
                segments.Add((text.Substring(position, start - position), false));
            segments.Add((text.Substring(start, end - start), true));
            position = end;
        }

        if (position < text.Length)
            segments.Add((text.Substring(position), false));

        return segments;
    }

    private static IEnumerable<(int Start, int End)> Normalize(int textLength, IEnumerable<HighlightRange>? ranges)
    {
        if (ranges == null)
            yield break;

        int? currentStart = null;
        var currentEnd = 0;
        foreach (var r in ranges.Where(r => r != null && r.Length > 0).OrderBy(r => r.Start))
        {
            var start = Math.Max(0, r.Start);
            var end = Math.Min(textLength, r.Start + r.Length);
            if (start >= end)
                continue;

            if (currentStart != null && start <= currentEnd)
            {
                currentEnd = Math.Max(currentEnd, end);
                continue;
            }

            if (currentStart != null)
                yield return (currentStart.Value, currentEnd);
            currentStart = start;
            currentEnd = end;
        }

        if (currentStart != null)
            yield return (currentStart.Value, currentEnd);
    }
}
