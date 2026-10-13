using System.Linq;
using BlazorDatasheet.Core.Clipboard;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Formula.Core;
using AwesomeAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Clipboard;

public class DelimitedTextTests
{
    [Test]
    public void Writer_Quotes_Only_Fields_That_Need_It()
    {
        var snapshot = new ClipboardSnapshot(2, 2);
        snapshot[0, 0] = new ClipboardCell { Value = CellValue.Text("plain") };
        snapshot[0, 1] = new ClipboardCell { Value = CellValue.Text("has\ttab") };
        snapshot[1, 0] = new ClipboardCell { Value = CellValue.Text("say \"hi\"") };
        snapshot[1, 1] = new ClipboardCell { Value = CellValue.Text("two\nlines") };

        DelimitedTextWriter.Write(snapshot).Should()
            .Be("plain\t\"has\ttab\"\r\n\"say \"\"hi\"\"\"\t\"two\nlines\"");
    }

    [Test]
    public void Writer_Formats_Values()
    {
        var snapshot = new ClipboardSnapshot(1, 4);
        snapshot[0, 0] = new ClipboardCell { Value = CellValue.Number(1.5) };
        snapshot[0, 1] = new ClipboardCell { Value = CellValue.Logical(true) };
        snapshot[0, 3] = new ClipboardCell { RawText = "raw" };
        DelimitedTextWriter.Write(snapshot).Should().Be("1.5\tTRUE\t\traw");
    }

    [Test]
    public void Parser_Reads_Quoted_Fields_With_Delimiters_Newlines_And_Doubled_Quotes()
    {
        var rows = DelimitedTextParser.Parse("a\t\"b\tc\"\r\n\"d\ne\"\t\"say \"\"hi\"\"\"\n");
        rows.Should().HaveCount(2);
        rows[0].Should().Equal("a", "b\tc");
        rows[1].Should().Equal("d\ne", "say \"hi\"");
    }

    [Test]
    public void Parser_Treats_An_Unmatched_Opening_Quote_As_Text()
    {
        var rows = DelimitedTextParser.Parse("\"\tfoo\nbar\tbaz");
        rows.Should().HaveCount(2);
        rows[0].Should().Equal("\"", "foo");
        rows[1].Should().Equal("bar", "baz");
    }

    [Test]
    public void Parser_Handles_Ragged_Rows_Blank_Rows_And_Literal_Quotes()
    {
        var rows = DelimitedTextParser.Parse("a\tb\tc\n\nx\n5\" pipe");
        rows.Should().HaveCount(4);
        rows[0].Should().Equal("a", "b", "c");
        rows[1].Should().Equal("");
        rows[2].Should().Equal("x");
        rows[3].Should().Equal("5\" pipe");
        DelimitedTextParser.Parse("").Should().BeEmpty();
        DelimitedTextParser.Parse("\n").Should().ContainSingle().Which.Should().Equal("");
    }

    [Test]
    public void Round_Trip_Through_Snapshot()
    {
        var text = "1\t\"a\nb\"\r\nTRUE\t";
        var snapshot = DelimitedTextParser.ToSnapshot(text);
        snapshot.Height.Should().Be(2);
        snapshot.Width.Should().Be(2);
        snapshot[0, 1]!.RawText.Should().Be("a\nb");
        snapshot[1, 1].Should().BeNull();
        DelimitedTextWriter.Write(snapshot).Should().Be(text);
    }

    [Test]
    public void InsertDelimitedText_Handles_Quoted_Multiline_Cells()
    {
        var sheet = new Sheet(5, 5);
        var region = sheet.InsertDelimitedText("1\t\"a\nb\"\r\nTRUE\t\"q \"\"x\"\"\"", new CellPosition(0, 0));
        region.Should().BeEquivalentTo(new Region(0, 1, 0, 1));
        sheet.Cells.GetValue(0, 0).Should().Be(1);
        sheet.Cells.GetValue(0, 1).Should().Be("a\nb");
        sheet.Cells.GetValue(1, 0).Should().Be(true);
        sheet.Cells.GetValue(1, 1).Should().Be("q \"x\"");
    }
}
