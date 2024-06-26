using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Layout;
using BlazorDatasheet.Render;
using FluentAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Layout;

public class CellLayoutProviderTests
{
    [Test]
    public void Setting_Width_Computes_Correct_Column_Width()
    {
        var sheet = new Sheet(3, 3);
        var w1 = sheet.Columns.DefaultSize;
        var w2 = 40;
        var provider = new CellLayoutProvider(sheet);
        Assert.AreEqual(w1, provider.ComputeWidth(1, 1));
        Assert.AreEqual(w1 * 3, provider.ComputeWidth(0, 3));
        sheet.Columns.SetSize(1, w2);
        Assert.AreEqual(2 * w1 + w2, provider.ComputeWidth(0, 3));
    }

    [Test]
    public void Set_Column_Width_Number_Of_Cols_Correct_Width()
    {
        var sheet = new Sheet(10, 10);
        sheet.Columns.SetSize(0, 5, 50);
        var p = new CellLayoutProvider(sheet);
        p.ComputeWidth(0, 1).Should().Be(50);
        p.ComputeWidth(1, 1).Should().Be(50);
        p.ComputeWidth(2, 1).Should().Be(50);
        p.ComputeWidth(3, 1).Should().Be(50);
        p.ComputeWidth(4, 1).Should().Be(50);
        p.ComputeWidth(5, 1).Should().Be(50);
    }

    [Test]
    public void Calculating_Width_And_Height_Width_Non_One_Span_Works()
    {
        var sheet = new Sheet(10, 10);
        var p = new CellLayoutProvider(sheet);
        p.ComputeWidth(1, 2).Should().Be(sheet.Columns.DefaultSize * 2);
        p.ComputeHeight(1, 3).Should().Be(sheet.Rows.DefaultSize * 3);
    }

    [Test]
    public void Inserting_Column_After_Setting_Width_Ends_With_Correct_Widths()
    {
        var sheet = new Sheet(3, 3);
        var defaultW = sheet.Columns.DefaultSize;
        var w2 = 40;
        var provider = new CellLayoutProvider(sheet);
        sheet.Columns.SetSize(1, w2);
        sheet.Columns.InsertAt(0);
        Assert.AreEqual(defaultW, provider.ComputeWidth(1, 1));
        Assert.AreEqual(w2, provider.ComputeWidth(2, 1));
        Assert.AreEqual(defaultW, provider.ComputeWidth(0, 1));
        sheet.Commands.Undo();
        Assert.AreEqual(w2, provider.ComputeWidth(1, 1));
        Assert.AreEqual(defaultW * 2 + w2, provider.ComputeWidth(0, 3));
        Assert.AreEqual(provider.TotalWidth, defaultW * 2 + w2);
    }

    [Test]
    public void Insert_Row_And_Remove_Row_Sets_Total_Height()
    {
        var sheet = new Sheet(3, 3);
        var dH = sheet.Rows.DefaultSize;
        var provider = new CellLayoutProvider(sheet);
        provider.TotalHeight.Should().Be(dH * 3);
        sheet.Rows.InsertAt(0);
        provider.TotalHeight.Should().Be(dH * 4);
        sheet.Rows.RemoveAt(0, 2);
        provider.TotalHeight.Should().Be(dH * 2);
    }

    [Test]
    public void Calculate_Column_With_Fixed_Widths_Calculates()
    {
        var sheet = new Sheet(5, 5);
        var dW = sheet.Columns.DefaultSize;
        var p = new CellLayoutProvider(sheet);
        p.IncludeRowHeadings = false;
        p.ComputeColumn(0).Should().Be(0);
        p.ComputeColumn(dW-1).Should().Be(0);
        p.ComputeColumn(dW+1).Should().Be(1);
    }

    [Test]
    public void Calculate_Column_With_Non_Fixed_Widths_Calculates_Correctly()
    {
        var sheet = new Sheet(5, 3);
        var dw = sheet.Columns.DefaultSize;
        var p = new CellLayoutProvider(sheet);
        p.IncludeRowHeadings = false;
        var nw = 40;
        sheet.Columns.SetSize(1, nw);
        p.ComputeColumn(dw-1).Should().Be(0);
        p.ComputeColumn(dw).Should().Be(1);
        p.ComputeColumn(dw+1).Should().Be(1);
        p.ComputeColumn(dw + nw - 1).Should().Be(1);
        p.ComputeColumn(dw + nw).Should().Be(2);
        p.ComputeColumn(dw + nw + 1).Should().Be(2);
        p.ComputeColumn(dw + nw + dw - 1).Should().Be(2);
    }

    [Test]
    public void Set_Multi_Column_Widths_Gets_Correct_Column()
    {
        var sheet = new Sheet(10, 10);
        sheet.Columns.SetSize(0, 5, 40);
        sheet.Columns.GetColumn(0).Should().Be(0);
        sheet.Columns.GetColumn(41).Should().Be(1);
    }
}