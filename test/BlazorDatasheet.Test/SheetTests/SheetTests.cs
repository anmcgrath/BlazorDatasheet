using System;
using System.Linq;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.DataStructures.Geometry;
using FluentAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.SheetTests;

public class SheetTests
{
    [Test]
    public void Create_Sheet_2x1_Has_Empty_Cells()
    {
        var sheet = new Sheet(2, 1);
        Assert.AreEqual(2, sheet.NumRows);
        Assert.AreEqual(null, sheet.Cells.GetCell(0, 0).GetValue());
        Assert.AreEqual(null, sheet.Cells.GetCell(1, 0).GetValue());
    }

    [Test]
    [TestCase(0, 1, 0, 1)]
    [TestCase(0, 1, 0, 0)]
    [TestCase(1, 2, 1, 1)]
    public void Get_delim_Data_from_Sheet(int copyPasteRegionR0, int copyPasteRegionR1, int copyPasteRegionC0,
        int copyPasteRegionC1)
    {
        var sheet = new Sheet(5, 5);
        var copyPasteRegion = new Region(copyPasteRegionR0, copyPasteRegionR1, copyPasteRegionC0, copyPasteRegionC1);

        foreach (var posn in copyPasteRegion)
            sheet.Cells.SetValue(posn.row, posn.col, getCellPosnString(posn.row, posn.col));

        var copy = sheet.GetRegionAsDelimitedText(copyPasteRegion);
        Assert.NotNull(copy);
        Assert.AreNotEqual(String.Empty, copy);

        // Clear the sheet so we are pasting over empty data
        sheet.Cells.ClearCells(sheet.Range(copyPasteRegion));

        var insertedRegions = sheet.InsertDelimitedText(copy, copyPasteRegion.TopLeft);

        Assert.NotNull(insertedRegions);
        Assert.True(insertedRegions!.Equals(copyPasteRegion));

        foreach (var posn in copyPasteRegion)
            Assert.AreEqual(getCellPosnString(posn.row, posn.col),
                sheet.Cells.GetCell(posn.row, posn.col).GetValue<string>());
    }

    private string getCellPosnString(int row, int col)
    {
        return $"({row},{col})";
    }

    [Test]
    public void Range_String_Specification_Tests()
    {
        var sheet = new Sheet(1, 1); // size doesn't matter, could be anything
        sheet["a1"].Regions.Should().ContainSingle(x => x.Left == 0 && x.Top == 0);
        sheet["b2"].Regions.Should().ContainSingle(x => x.Left == 1 && x.Top == 1);
        sheet["2a"].Regions.Should().BeEmpty();

        sheet["a1,b2"].Regions.Should().HaveCount(2);

        sheet["A1:B2"].Regions.Should()
            .ContainSingle(x => x.Left == 0 && x.Right == 1 && x.Top == 0 && x.Bottom == 1);

        sheet["B:C"].Regions.First().Should().BeOfType<ColumnRegion>();
        sheet["B:C"].Regions.Should().ContainSingle(x => x.Left == 1 && x.Right == 2);

        sheet["2:3"].Regions.First().Should().BeOfType<RowRegion>();
        sheet["2:3"].Regions.Should().ContainSingle(x => x.Top == 1 && x.Bottom == 2);
    }

    [Test]
    [TestCase("A_1")]
    [TestCase("asd")]
    [TestCase("asd..")]
    [TestCase("")]
    [TestCase("A,1")]
    [TestCase("A:1")]
    [TestCase("1")]
    [TestCase("A")]
    public void Bad_Range_Strings_return_Empty(string badText)
    {
        var sheet = new Sheet(1, 1);
        sheet[badText].Regions.Should().BeEmpty();
    }
}