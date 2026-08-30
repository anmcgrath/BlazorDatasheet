using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Render;
using FluentAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Render;

public class VisualCellIconTests
{
    [Test]
    public void Icon_Set_On_Cell_Format_Is_Visible_To_The_Renderer()
    {
        var sheet = new Sheet(2, 2);
        sheet.SetFormat(new Region(0, 0), new CellFormat { Icon = "tick", IconColor = "green" });

        var cell = new VisualCell(0, 0, sheet, 13);

        cell.Icon.Should().Be("tick");
        cell.Format!.IconColor.Should().Be("green");
    }

    [Test]
    public void Format_With_Only_An_Icon_Is_Not_A_Default_Format()
    {
        new CellFormat { Icon = "tick" }.IsDefaultFormat().Should().BeFalse();
    }

    [Test]
    public void Icon_From_A_Conditional_Format_Is_Visible_To_The_Renderer()
    {
        var sheet = new Sheet(2, 2);
        sheet.ConditionalFormats.Apply(new Region(0, 0), new IconConditionalFormat("warning"));

        var cell = new VisualCell(0, 0, sheet, 13);

        cell.Icon.Should().Be("warning");
    }

    [Test]
    public void Conditional_Format_Icon_Overrides_The_Cell_Format_Icon()
    {
        var sheet = new Sheet(2, 2);
        sheet.SetFormat(new Region(0, 0), new CellFormat { Icon = "tick" });
        sheet.ConditionalFormats.Apply(new Region(0, 0), new IconConditionalFormat("warning"));

        var cell = new VisualCell(0, 0, sheet, 13);

        cell.Icon.Should().Be("warning");
    }

    [Test]
    public void Cell_Without_An_Icon_Has_No_Icon()
    {
        var sheet = new Sheet(2, 2);
        sheet.SetFormat(new Region(0, 0), new CellFormat { BackgroundColor = "red" });

        new VisualCell(0, 0, sheet, 13).Icon.Should().BeNull();
    }

    private class IconConditionalFormat(string icon) : ConditionalFormatAbstractBase
    {
        public override CellFormat? CalculateFormat(int row, int col, Sheet sheet) =>
            new() { Icon = icon };
    }
}
