using BlazorDatasheet.Data;
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
        var w1 = 20;
        var w2 = 40;
        var provider = new CellLayoutProvider(sheet, w1, 10);
        Assert.AreEqual(w1, provider.ComputeWidth(1, 1));
        Assert.AreEqual(w1 * 3, provider.ComputeWidth(0, 3));
        provider.SetColumnWidth(1, w2);
        Assert.AreEqual(2 * w1 + w2, provider.ComputeWidth(0, 3));
    }

    [Test]
    public void Calculating_Width_And_Height_Width_Non_One_Span_Works()
    {
        var sheet = new Sheet(10, 10);
        var p = new CellLayoutProvider(sheet, 20, 30);
        p.ComputeWidth(1, 2).Should().Be(40);
        p.ComputeHeight(3).Should().Be(90);
    }

    [Test]
    public void Inserting_Column_After_Setting_Width_Ends_With_Correct_Widths()
    {
        var sheet = new Sheet(3, 3);
        var defaultW = 20;
        var w2 = 40;
        var provider = new CellLayoutProvider(sheet, defaultW, 10);
        provider.SetColumnWidth(1, w2);
        sheet.InsertColAt(0);
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
        var dH = 20;
        var provider = new CellLayoutProvider(sheet, 20, dH);
        provider.TotalHeight.Should().Be(dH * 3);
        sheet.InsertRowAt(0);
        provider.TotalHeight.Should().Be(dH * 4);
        sheet.RemoveRow(0, 2);
        provider.TotalHeight.Should().Be(dH * 2);
    }

    [Test]
    public void Calculate_Column_With_Fixed_Widths_Calculates()
    {
        var sheet = new Sheet(5, 5);
        sheet.ShowRowHeadings = false;
        var p = new CellLayoutProvider(sheet, 20, 20);
        p.ComputeColumn(0, false).Should().Be(0);
        p.ComputeColumn(19, false).Should().Be(0);
        p.ComputeColumn(21, false).Should().Be(1);
    }

    [Test]
    public void Calculate_Column_With_Non_Fixed_Widths_Calculates_Correctly()
    {
        var sheet = new Sheet(5, 3);
        sheet.ShowRowHeadings = false;
        var p = new CellLayoutProvider(sheet, 20, 20);
        p.SetColumnWidth(1, 40);
        p.ComputeColumn(19, false).Should().Be(0);
        p.ComputeColumn(20, false).Should().Be(1);
        p.ComputeColumn(21, false).Should().Be(1);
        p.ComputeColumn(59, false).Should().Be(1);
        p.ComputeColumn(60, false).Should().Be(2);
        p.ComputeColumn(61, false).Should().Be(2);
        p.ComputeColumn(99, false).Should().Be(3);
    }

    [Test]
    public void Calculate_Column_With_Sheet_Row_Headers_Calculates_Correctly()
    {
        var sheet = new Sheet(5, 5);
        sheet.ShowRowHeadings = true;
        var p = new CellLayoutProvider(sheet, 20, 20);
        p.ComputeColumn(1, true).Should().Be(-1);
        p.ComputeColumn(20, true).Should().Be(0);
    }
    
    [Test]
    public void Calculate_Row_With_Sheet_Col_Headers_Calculates_Correctly()
    {
        var sheet = new Sheet(5, 5);
        sheet.ShowColumnHeadings = true;
        var p = new CellLayoutProvider(sheet, 20, 20);
        p.ComputeRow(0, true).Should().Be(-1);
        p.ComputeRow(20, true).Should().Be(0);
    }
}