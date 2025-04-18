using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.Render;
using FluentAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Render;

public class VisualCellTests
{
    [Test]
    public void Text_Wrap_Appears_With_Vertical_Format_Set()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells["A1"]!.Format = new CellFormat()
        {
            VerticalTextAlign = TextAlign.Center,
            TextWrap = TextWrapping.Wrap
        };
        var vc = new VisualCell(0, 0, sheet, 12);
        vc.FormatStyleString.Should().Contain("text-wrap");
    }
}