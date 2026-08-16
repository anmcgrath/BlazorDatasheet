using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.DataStructures.Geometry;
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

    [Test]
    public void Plain_Number_Uses_Shared_Alignment_Class_Without_Inline_Style_Or_Format()
    {
        var sheet = new Sheet(1, 1);
        sheet.Cells.SetValue(0, 0, 123);

        var cell = new VisualCell(0, 0, sheet, 12);

        cell.ClassString.Should().Be("bds-sheet-cell bds-cell-align-end");
        cell.FormatStyleString.Should().BeEmpty();
        cell.Format.Should().BeNull();
    }

    [Test]
    public void Explicit_Alignment_Uses_Shared_Classes_And_Other_Formatting_Remains_Inline()
    {
        var sheet = new Sheet(1, 1);
        sheet.SetFormat(new Region(0, 0), new CellFormat
        {
            HorizontalTextAlign = TextAlign.Center,
            VerticalTextAlign = TextAlign.End,
            BackgroundColor = "red"
        });

        var cell = new VisualCell(0, 0, sheet, 12);

        cell.ClassString.Should().Be("bds-sheet-cell bds-cell-align-center bds-cell-valign-end");
        cell.FormatStyleString.Should().Contain("background-color: red");
        cell.FormatStyleString.Should().NotContain("text-align");
        cell.FormatStyleString.Should().NotContain("align-items");
    }

    [Test]
    public void Merged_Cell_Dimensions_Include_Visible_Cells_After_Hidden_Internal_Row_And_Column()
    {
        var sheet = new Sheet(3, 3);
        sheet.Rows.SetSize(0, 20);
        sheet.Rows.SetSize(1, 30);
        sheet.Rows.SetSize(2, 40);
        sheet.Columns.SetSize(0, 50);
        sheet.Columns.SetSize(1, 60);
        sheet.Columns.SetSize(2, 70);
        sheet.Cells.Merge(new Region(0, 2, 0, 2));
        sheet.Rows.Hide(1, 1);
        sheet.Columns.Hide(1, 1);

        var cell = new VisualCell(0, 0, sheet, 12);

        cell.VisibleMergeRowStart.Should().Be(0);
        cell.VisibleMergeColStart.Should().Be(0);
        cell.VisibleRowSpan.Should().Be(2);
        cell.VisibleColSpan.Should().Be(2);
        cell.Height.Should().Be(60);
        cell.Width.Should().Be(120);
    }

    [Test]
    public void Merged_Cell_Dimensions_Use_First_Visible_Row_And_Column_When_Leading_Ones_Are_Hidden()
    {
        var sheet = new Sheet(3, 3);
        sheet.Rows.SetSize(0, 20);
        sheet.Rows.SetSize(1, 30);
        sheet.Rows.SetSize(2, 40);
        sheet.Columns.SetSize(0, 50);
        sheet.Columns.SetSize(1, 60);
        sheet.Columns.SetSize(2, 70);
        sheet.Cells.Merge(new Region(0, 2, 0, 2));
        sheet.Rows.Hide(0, 1);
        sheet.Columns.Hide(0, 1);

        var cell = new VisualCell(0, 0, sheet, 12);

        cell.VisibleMergeRowStart.Should().Be(1);
        cell.VisibleMergeColStart.Should().Be(1);
        cell.VisibleRowSpan.Should().Be(2);
        cell.VisibleColSpan.Should().Be(2);
        cell.Height.Should().Be(70);
        cell.Width.Should().Be(130);
    }
}
