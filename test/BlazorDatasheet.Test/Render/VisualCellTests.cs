using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Render;
using FluentAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Render;

public class VisualCellTests
{
    // narrow enough that a number cannot be assumed to fit, whatever the font
    private const int NarrowColumnWidth = 24;

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

        cell.ClassString.Should().Be("bds-sheet-cell bds-cell-align-end bds-cell-number");
        cell.IsNumber.Should().BeTrue();
        cell.FormatStyleString.Should().BeEmpty();
        cell.Format.Should().BeNull();
    }

    [Test]
    public void Explicitly_End_Aligned_Text_Is_Not_Marked_As_Number()
    {
        var sheet = new Sheet(1, 1);
        sheet.Cells.SetValue(0, 0, "right-aligned text");
        sheet.SetFormat(new Region(0, 0), new CellFormat
        {
            HorizontalTextAlign = TextAlign.End
        });

        var cell = new VisualCell(0, 0, sheet, 12);

        cell.ClassString.Should().Be("bds-sheet-cell bds-cell-align-end");
        cell.IsNumber.Should().BeFalse();
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

    [TestCase(123.456, new[] { "123.46", "123.5", "123" })]
    [TestCase(-123.456, new[] { "-123.46", "-123.5", "-123" })]
    [TestCase(77.3336, new[] { "77.334", "77.33", "77.3", "77" })]
    [TestCase(123.01, new[] { "123" })]
    [TestCase(999.9, new[] { "1000" })]
    [TestCase(99.9, new[] { "100" })]
    [TestCase(1.2345e20, new[] { "1.235E+20", "1.23E+20", "1.2E+20", "1E+20" })]
    public void General_Number_Has_Complete_Distinct_Roundings(double number, string[] expected)
    {
        var sheet = new Sheet(1, 1);
        sheet.Columns.SetSize(0, NarrowColumnWidth);
        sheet.Cells.SetValue(0, 0, number);

        var cell = new VisualCell(0, 0, sheet, 13);

        cell.NumberFallbacks.Should().Equal(expected);
    }

    [TestCase(0, new[] { "0.666667", "0.66667", "0.6667", "0.667", "0.67", "0.7", "1" })]
    [TestCase(5, new[] { "0.666667", "0.66667" })]
    [TestCase(7, new string[0])]
    public void General_Number_Tries_Every_Precision_Down_To_The_Minimum(int minDecimals, string[] expected)
    {
        var sheet = new Sheet(1, 1);
        sheet.Columns.SetSize(0, NarrowColumnWidth);
        sheet.Cells.SetValue(0, 0, 0.6666667);

        var cell = new VisualCell(0, 0, sheet, 13,
            new NumberOverflowOptions(NumberOverflowMode.RoundToFit, minDecimals));

        cell.NumberFallbacks.Should().Equal(expected);
    }

    [Test]
    public void A_Number_With_Room_To_Spare_Has_No_Fallbacks()
    {
        var sheet = new Sheet(1, 1);
        sheet.Columns.SetSize(0, 500);
        sheet.Cells.SetValue(0, 0, 123.456);

        new VisualCell(0, 0, sheet, 13).NumberFallbacks.Should().BeEmpty();
    }

    [TestCase(123456)]
    [TestCase(1e20)]
    [TestCase(double.PositiveInfinity)]
    [TestCase(double.NaN)]
    public void Numbers_Without_A_Roundable_Fraction_Have_No_Fallbacks(double number)
    {
        var sheet = new Sheet(1, 1);
        sheet.Cells.SetValue(0, 0, number);

        new VisualCell(0, 0, sheet, 13).NumberFallbacks.Should().BeEmpty();
    }

    [TestCase(NumberOverflowMode.Hashes)]
    [TestCase(NumberOverflowMode.Clip)]
    public void Number_Has_No_Fallbacks_Unless_Rounding_To_Fit(NumberOverflowMode mode)
    {
        var sheet = new Sheet(1, 1);
        sheet.Cells.SetValue(0, 0, 123.456);

        var cell = new VisualCell(0, 0, sheet, 13, new NumberOverflowOptions(mode, 0));

        cell.FormattedString.Should().Be("123.456");
        cell.NumberFallbacks.Should().BeEmpty();
    }

    [TestCase("0.00")]
    [TestCase("0.00E+00")]
    [TestCase("0 \"v1.5\"")]
    public void Explicit_Number_Formats_Have_No_Rounded_Fallbacks(string numberFormat)
    {
        var sheet = new Sheet(1, 1);
        sheet.Cells.SetValue(0, 0, 123.456);
        sheet.SetFormat(new Region(0, 0), new CellFormat { NumberFormat = numberFormat });

        new VisualCell(0, 0, sheet, 13).NumberFallbacks.Should().BeEmpty();
    }

    [Test]
    public void Resizing_Works_Out_All_The_Roundings_For_The_New_Width()
    {
        var sheet = new Sheet(1, 1);
        sheet.Cells.SetValue(0, 0, 123.456);
        sheet.Columns.SetSize(0, 500);
        var cell = new VisualCell(0, 0, sheet, 13);

        sheet.Columns.SetSize(0, 20);
        cell.RefreshAxisMetrics(sheet, AxisMetrics.ForRow(sheet, 0), AxisMetrics.ForColumn(sheet, 0));

        cell.Width.Should().Be(20);
        cell.NumberFallbacks.Should().Equal("123.46", "123.5", "123");
    }
}
