using System;
using System.Linq;
using BlazorDatasheet.Core.Clipboard;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Formula.Core;
using AwesomeAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Clipboard;

public class ClipboardHtmlTests
{
    [Test]
    public void Own_Html_Round_Trips_Values_Types_Formulas_Formats_And_Merges()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells.SetValue(0, 0, 1.25);
        sheet.Cells.SetValue(0, 1, "text <b>&amp;</b>");
        sheet.Cells.SetValue(1, 0, true);
        sheet.Cells.SetValue(1, 1, new DateTime(2024, 3, 4, 5, 6, 7));
        sheet.Cells.SetFormula(2, 0, "=A1*2");
        sheet.Cells.SetType(new Region(2, 1), "text");
        sheet.Cells.SetValue(2, 1, "007");
        sheet.SetFormat(new Region(0, 0), new CellFormat
        {
            FontWeight = "bold", FontStyle = "italic", TextDecoration = "underline",
            ForegroundColor = "#ff0000", BackgroundColor = "#00ff00",
            HorizontalTextAlign = TextAlign.Center, VerticalTextAlign = TextAlign.End,
            BorderBottom = new Border { Width = 2, Color = "#0000ff" }, NumberFormat = "0.00",
            TextWrap = TextWrapping.Wrap, IsLocked = true, CssClass = "secret"
        });
        sheet.Cells.Merge(new Region(3, 4, 0, 1));
        sheet.Cells.SetValue(3, 0, "merged\nlines");

        var id = Guid.NewGuid();
        var snapshot = ClipboardSnapshotBuilder.FromRange(sheet.Range("A1:B5")!, id);
        var html = ClipboardHtmlWriter.Write(snapshot);

        html.Should().Contain("data-bds-clipboard-id=\"" + id.ToString("N") + "\"");
        html.Should().Contain("rowspan=\"2\"").And.Contain("colspan=\"2\"");
        html.Should().Contain("text &lt;b&gt;&amp;amp;&lt;/b&gt;");
        html.Should().NotContain("secret");
        html.Should().Contain("merged<br>lines");

        ClipboardHtmlParser.TryGetClipboardId(html, out var parsedId).Should().BeTrue();
        parsedId.Should().Be(id);

        var parsed = ClipboardHtmlParser.Parse(html)!;
        parsed.Height.Should().Be(5);
        parsed.Width.Should().Be(2);
        parsed.SourceId.Should().Be(id);
        parsed[0, 0]!.Value.Should().Be(CellValue.Number(1.25));
        parsed[0, 1]!.Value.Should().Be(CellValue.Text("text <b>&amp;</b>"));
        parsed[1, 0]!.Value.Should().Be(CellValue.Logical(true));
        parsed[1, 1]!.Value.Data.Should().Be(new DateTime(2024, 3, 4, 5, 6, 7));
        parsed[2, 0]!.Formula.Should().Be("=A1*2");
        parsed[2, 1]!.Type.Should().Be("text");
        parsed[2, 1]!.Value.Should().Be(CellValue.Text("007"));
        parsed[3, 0]!.Value.Should().Be(CellValue.Text("merged\nlines"));
        parsed.Origin.Should().Be(new CellPosition(0, 0));
        html.Should().Contain("data-bds-origin=\"0,0\"");
        parsed.Merges.Should().ContainSingle().Which.Should().BeEquivalentTo(new Region(3, 4, 0, 1));

        var format = parsed[0, 0]!.Format!;
        format.FontWeight.Should().Be("bold");
        format.FontStyle.Should().Be("italic");
        format.TextDecoration.Should().Be("underline");
        format.ForegroundColor.Should().Be("#ff0000");
        format.BackgroundColor.Should().Be("#00ff00");
        format.HorizontalTextAlign.Should().Be(TextAlign.Center);
        format.VerticalTextAlign.Should().Be(TextAlign.End);
        format.BorderBottom.Should().BeEquivalentTo(new Border { Width = 2, Color = "#0000ff" });
        format.NumberFormat.Should().Be("0.00");
        format.TextWrap.Should().Be(TextWrapping.Wrap);
        format.IsLocked.Should().BeNull();
    }

    [Test]
    public void Parses_Excel_Desktop_Html_With_Class_Styles()
    {
        const string html = """
            Version:1.0
            StartHTML:0000000105
            <html xmlns:o="urn:schemas-microsoft-com:office:office" xmlns:x="urn:schemas-microsoft-com:office:excel">
            <head><style>
            <!--table
            	{mso-displayed-decimal-separator:"\.";}
            .xl65
            	{color:red;
            	font-weight:700;
            	border:.5pt solid windowtext;
            	mso-number-format:"0\.00";
            	background:yellow;
            	text-align:center;}
            td.xl66, .xl67
            	{font-style:italic;}
            -->
            </style></head>
            <body link="#0563C1"><!--StartFragment-->
            <table border=0 cellpadding=0 cellspacing=0 width=128>
             <col width=64 span=2>
             <tr height=19>
              <td height=19 class=xl65 width=64 align=right x:num="42.5">42.5</td>
              <td class=xl66 width=64>Hello<o:p></o:p></td>
             </tr>
             <tr height=19>
              <td colspan=2 rowspan=2 height=38 style='font-weight:700'>Merged&nbsp;cell</td>
             </tr>
             <tr height=19></tr>
            </table>
            <!--EndFragment--></body></html>
            """;

        ClipboardHtmlParser.TryGetClipboardId(html, out _).Should().BeFalse();
        var snapshot = ClipboardHtmlParser.Parse(html)!;
        snapshot.Height.Should().Be(3);
        snapshot.Width.Should().Be(2);

        var a1 = snapshot[0, 0]!;
        a1.Value.Should().Be(CellValue.Number(42.5));
        a1.Format!.ForegroundColor.Should().Be("red");
        a1.Format.FontWeight.Should().Be("bold");
        a1.Format.BackgroundColor.Should().Be("yellow");
        a1.Format.HorizontalTextAlign.Should().Be(TextAlign.Center);
        a1.Format.NumberFormat.Should().Be("0.00");
        a1.Format.BorderLeft!.Color.Should().Be("#000000");
        a1.Format.BorderTop!.Width.Should().Be(1);

        var b1 = snapshot[0, 1]!;
        b1.RawText.Should().Be("Hello");
        b1.Format!.FontStyle.Should().Be("italic");

        var a2 = snapshot[1, 0]!;
        a2.RawText.Should().Be("Merged cell");
        a2.Format!.FontWeight.Should().Be("bold");
        snapshot.Merges.Should().ContainSingle().Which.Should().BeEquivalentTo(new Region(1, 2, 0, 1));
    }

    [Test]
    public void Parses_Google_Sheets_Html()
    {
        const string html = """
            <meta charset='utf-8'><google-sheets-html-origin><style type="text/css"><!--td {border: 1px solid #cccccc;}br {mso-data-placement:same-cell;}--></style>
            <table xmlns="http://www.w3.org/1999/xhtml" cellspacing="0" cellpadding="0" dir="ltr" border="1" style="table-layout:fixed;font-size:10pt;font-family:Arial;width:0px;border-collapse:collapse;border:none" data-sheets-root="1">
            <colgroup><col width="100"/><col width="100"/></colgroup>
            <tbody>
            <tr style="height:21px;">
            <td style="overflow:hidden;padding:2px 3px 2px 3px;vertical-align:bottom;font-weight:bold;background-color:#ffff00;" data-sheets-value="{&quot;1&quot;:3,&quot;3&quot;:12}">12</td>
            <td style="overflow:hidden;padding:2px 3px 2px 3px;vertical-align:bottom;text-decoration:underline;color:#ff0000;" data-sheets-value="{&quot;1&quot;:2,&quot;2&quot;:&quot;hi there&quot;}">hi there</td>
            </tr>
            <tr style="height:21px;">
            <td style="overflow:hidden;" data-sheets-value="{&quot;1&quot;:4,&quot;4&quot;:true}">TRUE</td>
            <td style="overflow:hidden;" rowspan="1" colspan="1"></td>
            </tr>
            </tbody></table>
            """;

        var snapshot = ClipboardHtmlParser.Parse(html)!;
        snapshot.Height.Should().Be(2);
        snapshot.Width.Should().Be(2);
        snapshot[0, 0]!.Value.Should().Be(CellValue.Number(12));
        snapshot[0, 0]!.Format!.FontWeight.Should().Be("bold");
        snapshot[0, 0]!.Format!.BackgroundColor.Should().Be("#ffff00");
        snapshot[0, 0]!.Format!.VerticalTextAlign.Should().Be(TextAlign.End);
        snapshot[0, 1]!.Value.Should().Be(CellValue.Text("hi there"));
        snapshot[0, 1]!.Format!.TextDecoration.Should().Be("underline");
        snapshot[0, 1]!.Format!.ForegroundColor.Should().Be("#ff0000");
        snapshot[1, 0]!.Value.Should().Be(CellValue.Logical(true));
        snapshot[1, 1].Should().BeNull();
        snapshot.Merges.Should().BeEmpty();
    }

    [Test]
    public void Inline_Tags_And_Unclosed_Cells_Are_Tolerated()
    {
        const string html = "<table><tr><td><b>bold</b> text<td><i>it</i><tr><td><font color=\"#123456\">c</font></table>";
        var snapshot = ClipboardHtmlParser.Parse(html)!;
        snapshot.Height.Should().Be(2);
        snapshot.Width.Should().Be(2);
        snapshot[0, 0]!.RawText.Should().Be("bold text");
        snapshot[0, 0]!.Format!.FontWeight.Should().Be("bold");
        snapshot[0, 1]!.Format!.FontStyle.Should().Be("italic");
        snapshot[1, 0]!.Format!.ForegroundColor.Should().Be("#123456");
    }

    [Test]
    public void No_Table_Returns_Null()
    {
        ClipboardHtmlParser.Parse("<p>just text</p>").Should().BeNull();
        ClipboardHtmlParser.Parse(null).Should().BeNull();
        ClipboardHtmlParser.ContainsTable("<p>x</p>").Should().BeFalse();
        ClipboardHtmlParser.ContainsTable("<TABLE><tr><td>1</td></tr></TABLE>").Should().BeTrue();
    }
}
