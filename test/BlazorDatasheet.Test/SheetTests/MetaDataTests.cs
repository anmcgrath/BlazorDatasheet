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
    public void Clear_Cell_MetaData_Key_And_Undo_Works()
    {
        var sheet = new Sheet(3, 3);
        sheet.Cells.SetCellMetaData(1, 1, "test", 7);
        sheet.Cells.SetCellMetaData(1, 1, "other", 8);

        sheet.Cells.ClearCellMetaData(1, 1, "test");

        sheet.Cells.GetMetaData(1, 1, "test").Should().BeNull();
        sheet.Cells.GetMetaData(1, 1, "other").Should().Be(8);

        sheet.Commands.Undo();

        sheet.Cells.GetMetaData(1, 1, "test").Should().Be(7);
        sheet.Cells.GetMetaData(1, 1, "other").Should().Be(8);
    }

    [Test]
    public void Setting_Cell_MetaData_To_Null_Clears_Key()
    {
        var sheet = new Sheet(3, 3);
        sheet.Cells.SetCellMetaData(1, 1, "test", 7);
        sheet.Cells.SetCellMetaData(1, 1, "other", 8);

        sheet.Cells.SetCellMetaData(1, 1, "test", null);

        sheet.Cells.GetMetaData(1, 1, "test").Should().BeNull();
        sheet.Cells.GetMetaData(1, 1, "other").Should().Be(8);
    }

    [Test]
    public void Clear_Cell_MetaData_And_Undo_Works()
    {
        var sheet = new Sheet(3, 3);
        sheet.Cells.SetCellMetaData(1, 1, "test", 7);
        sheet.Cells.SetCellMetaData(1, 1, "other", 8);

        sheet.Cells.ClearCellMetaData(1, 1);

        sheet.Cells.GetMetaData(1, 1, "test").Should().BeNull();
        sheet.Cells.GetMetaData(1, 1, "other").Should().BeNull();

        sheet.Commands.Undo();

        sheet.Cells.GetMetaData(1, 1, "test").Should().Be(7);
        sheet.Cells.GetMetaData(1, 1, "other").Should().Be(8);
    }

    [Test]
    public void Sheet_Cell_Can_Set_And_Clear_MetaData()
    {
        var sheet = new Sheet(3, 3);
        var cell = sheet.Cells[1, 1];

        cell.SetMetaData("test", 7);
        cell.SetMetaData("other", 8);
        cell.ClearMetaData("test");

        cell.GetMetaData("test").Should().BeNull();
        cell.GetMetaData("other").Should().Be(8);

        cell.ClearMetaData();

        cell.MetaData.Should().BeEmpty();
    }

    [Test]
    public void Sheet_Range_Can_Clear_Specific_And_All_MetaData()
    {
        var sheet = new Sheet(3, 3);
        var range = sheet.Range(0, 1, 0, 1);
        range.SetMetaData("test", 7);
        range.SetMetaData("other", 8);

        range.ClearMetaData("test");

        sheet.Cells.GetMetaData(0, 0, "test").Should().BeNull();
        sheet.Cells.GetMetaData(1, 1, "test").Should().BeNull();
        sheet.Cells.GetMetaData(0, 0, "other").Should().Be(8);
        sheet.Cells.GetMetaData(1, 1, "other").Should().Be(8);

        range.ClearMetaData();

        sheet.Cells.GetMetaData(0, 0, "other").Should().BeNull();
        sheet.Cells.GetMetaData(1, 1, "other").Should().BeNull();
    }

    [Test]
    public void MetaData_Changed_Event_Reports_Key_Old_And_New_Values()
    {
        var sheet = new Sheet(3, 3);
        var changes = new List<(string Name, object? OldValue, object? NewValue)>();
        sheet.Cells.MetaDataChanged += (_, args) => changes.Add((args.Name, args.OldValue, args.NewValue));

        sheet.Cells.SetCellMetaData(1, 1, "test", 7);
        sheet.Cells.SetCellMetaData(1, 1, "test", 8);
        sheet.Cells.SetCellMetaData(1, 1, "other", 9);
        sheet.Cells.ClearCellMetaData(1, 1, "test");
        sheet.Cells.ClearCellMetaData(1, 1);

        var expected = new List<(string Name, object? OldValue, object? NewValue)>
        {
            ("test", null, 7),
            ("test", 7, 8),
            ("other", null, 9),
            ("test", 8, null),
            ("other", 9, null)
        };
        changes.Should().Equal(expected);
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
        sheet.Cells[0, 0].Value = 10;
        sheet.Cells[1, 0].Value = 5;
        sheet.Cells.SetCellMetaData(0, 0, "test", "testMd");
        sheet.SortRange(new ColumnRegion(0), [new(0, true)]);
        sheet.Cells.GetMetaData(1, 0, "test").Should().Be("testMd");
        sheet.Commands.Undo();
        sheet.Cells.GetMetaData(0, 0, "test").Should().Be("testMd");
    }
}
