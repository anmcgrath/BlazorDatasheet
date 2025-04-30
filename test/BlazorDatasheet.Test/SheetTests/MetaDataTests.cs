using System.Collections.Generic;
using BlazorDatasheet.Core.Commands.Data;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.DataStructures.Geometry;
using FluentAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.SheetTests;

public class MetaDataTests
{
    [Test]
    public void Set_Cell_MetaData_And_Undo_Works()
    {
        var sheet = new Sheet(3, 3);
        sheet.Cells.SetCellMetaData(1, 1, "test", 7);
        Assert.AreEqual(7, sheet.Cells.GetMetaData(1, 1, "test"));
        sheet.Cells.SetCellMetaData(1, 1, "test", 8);
        Assert.AreEqual(8, sheet.Cells.GetMetaData(1, 1, "test"));
        sheet.Commands.Undo();
        Assert.AreEqual(7, sheet.Cells.GetMetaData(1, 1, "test"));
    }

    [Test]
    public void Insert_Row_Shifts_MetaData()
    {
        var sheet = new Sheet(100, 100);
        sheet.Cells.SetCellMetaData(1, 1, "test", 7);
        sheet.Rows.InsertAt(0);
        sheet.Cells.GetMetaData(1, 1, "test").Should().BeNull();
        sheet.Cells.GetMetaData(2, 1, "test").Should().Be(7);
        sheet.Commands.Undo();
        sheet.Cells.GetMetaData(2, 1, "test").Should().BeNull();
        sheet.Cells.GetMetaData(1, 1, "test").Should().Be(7);
    }

    [Test]
    public void Delete_Rows_Shifts_MetaData()
    {
        var sheet = new Sheet(100, 100);
        sheet.Cells.SetCellMetaData(1, 1, "test", 7);
        sheet.Rows.RemoveAt(0);
        sheet.Cells.GetMetaData(1, 1, "test").Should().BeNull();
        sheet.Cells.GetMetaData(0, 1, "test").Should().Be(7);
        sheet.Commands.Undo();
        sheet.Cells.GetMetaData(0, 1, "test").Should().BeNull();
        sheet.Cells.GetMetaData(1, 1, "test").Should().Be(7);
    }

    [Test]
    public void Metadata_Moves_Correctly_With_Sort()
    {
        var sheet = new Sheet(100, 100);
        sheet.Cells["A1"].Value = 10;
        sheet.Cells["A2"].Value = 5;
        sheet.Cells.SetCellMetaData(0, 0, "test", "testMd");
        sheet.SortRange(new ColumnRegion(0), [new(0, true)]);
        sheet.Cells.GetMetaData(1, 0, "test").Should().Be("testMd");
        sheet.Commands.Undo();
        sheet.Cells.GetMetaData(0, 0, "test").Should().Be("testMd");
    }
}