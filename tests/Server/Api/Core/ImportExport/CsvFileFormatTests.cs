using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevInstance.DevCoreApp.Server.Services.Core.ImportExport.Generation;
using DevInstance.DevCoreApp.Server.Services.Core.ImportExport.Parsing;
using Xunit;

namespace DevInstance.DevCoreApp.Server.Tests.Core.ImportExport;

/// <summary>
/// Pins what the CSV import and export accept and produce, independent of CsvHelper's defaults:
/// those changed in 34 (separator sniffing, repairing malformed input) and a package update must
/// not change how a customer's file is read.
/// </summary>
public class CsvFileFormatTests
{
    private static MemoryStream Csv(string text) => new(Encoding.UTF8.GetBytes(text));

    private static async Task<string> GenerateAsync(List<string> headers, params Dictionary<string, string?>[] rows)
    {
        using var stream = await new CsvFileGenerator().GenerateAsync(headers, rows.ToList());
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }

    [Fact]
    public async Task Parser_ReadsQuotedFieldsAndSkipsBlankLines()
    {
        using var stream = Csv("a,b\r\n\r\n\"x,y\",\"he said \"\"hi\"\"\"\r\n\"line1\nline2\",z\r\n");

        var parser = new CsvFileParser();

        Assert.Equal(new[] { "a", "b" }, await parser.ParseHeadersAsync(stream));
        var rows = await parser.ParseRowsAsync(stream);
        Assert.Equal(2, rows.Count);
        Assert.Equal(new[] { "x,y", "he said \"hi\"" }, rows[0]);
        Assert.Equal(new[] { "line1\nline2", "z" }, rows[1]);
    }

    [Fact]
    public async Task Parser_LeavesStreamOpenAndReadableAgain()
    {
        using var stream = Csv("a\r\n1\r\n");
        var parser = new CsvFileParser();

        await parser.ParseHeadersAsync(stream);
        var rows = await parser.ParseRowsAsync(stream);

        Assert.True(stream.CanRead);
        Assert.Single(rows);
    }

    [Fact]
    public async Task Parser_StripsByteOrderMark()
    {
        var bytes = new byte[] { 0xEF, 0xBB, 0xBF }.Concat(Encoding.UTF8.GetBytes("a,b\r\n1,2\r\n")).ToArray();
        using var stream = new MemoryStream(bytes);

        Assert.Equal(new[] { "a", "b" }, await new CsvFileParser().ParseHeadersAsync(stream));
    }

    [Theory]
    [InlineData("a;b\r\n1;2\r\n", "a;b")]
    [InlineData("sep=;\r\na;b\r\n", "sep=;")]
    public async Task Parser_DoesNotDetectAnotherSeparator(string text, string expectedHeader)
    {
        using var stream = Csv(text);

        Assert.Equal(new[] { expectedHeader }, await new CsvFileParser().ParseHeadersAsync(stream));
    }

    [Theory]
    [InlineData("a,b,c\r\n1,2\r\n")]      // short row
    [InlineData("a,b\r\nx\"y,2\r\n")]     // stray quote
    [InlineData("a,b\r\n\"x,2\r\n3,4\r\n")] // unclosed quote
    public async Task Parser_RejectsMalformedRows(string text)
    {
        using var stream = Csv(text);

        await Assert.ThrowsAnyAsync<Exception>(() => new CsvFileParser().ParseRowsAsync(stream));
    }

    [Fact]
    public async Task Parser_IgnoresFieldsBeyondTheHeader()
    {
        using var stream = Csv("a,b\r\n1,2,3\r\n");

        var rows = await new CsvFileParser().ParseRowsAsync(stream);

        Assert.Equal(new[] { "1", "2" }, Assert.Single(rows));
    }

    [Fact]
    public async Task Generator_QuotesOnlyWhatNeedsIt()
    {
        var csv = await GenerateAsync(
            new List<string> { "a", "b" },
            new() { ["a"] = "x,y", ["b"] = "q\"r" },
            new() { ["a"] = " padded ", ["b"] = "inner space" },
            new() { ["a"] = "=SUM(A1)", ["b"] = null });

        Assert.Equal("a,b\r\n\"x,y\",\"q\"\"r\"\r\n\" padded \",inner space\r\n=SUM(A1),\r\n", csv);
    }

    [Fact]
    public async Task Generator_WritesNoByteOrderMark()
    {
        using var stream = await new CsvFileGenerator().GenerateAsync(
            new List<string> { "a" }, new List<Dictionary<string, string?>>());

        Assert.Equal((byte)'a', stream.ReadByte());
    }

    [Fact]
    public async Task Generator_WithNoHeaders_ProducesEmptyFile()
    {
        Assert.Equal(string.Empty, await GenerateAsync(new List<string>()));
    }

    [Fact]
    public async Task Generator_OutputReadsBackThroughParser()
    {
        using var stream = await new CsvFileGenerator().GenerateAsync(
            new List<string> { "a", "b" },
            new List<Dictionary<string, string?>> { new() { ["a"] = "l1\nl2", ["b"] = " é, \"x\" " } });

        var parser = new CsvFileParser();

        Assert.Equal(new[] { "a", "b" }, await parser.ParseHeadersAsync(stream));
        Assert.Equal(new[] { "l1\nl2", " é, \"x\" " }, Assert.Single(await parser.ParseRowsAsync(stream)));
    }
}
