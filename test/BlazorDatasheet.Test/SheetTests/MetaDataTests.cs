using System.Collections.Generic;
using System.Linq;
using BlazorDatasheet.Core.Commands.Data;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.DataStructures.Geometry;
using FluentAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.SheetTests;

public class MetaDataTests
{
    [TestCase("ready")]
    [TestCase(null)]
    public void Region_MetaData_Preserves_Other_Keys_And_Undoes_As_One_Operation(string? value)
    {
        var sheet = new Sheet(4, 4);
        var region = new Region(1, 2, 1, 2);
        sheet.Cells.SetCellMetaData(1, 1, "status", "old");
        sheet.Cells.SetCellMetaData(2, 2, "status", 7);
        sheet.Cells.SetCellMetaData(1, 2, "other", "keep");
        sheet.Cells.SetCellMetaData(0, 0, "status", "outside");

        sheet.Cells.SetCellMetaData(region, "status", value).Should().BeTrue();
        foreach (var position in sheet.Range(region).Positions)
            sheet.Cells.GetMetaData(position.row, position.col, "status").Should().Be(value);

        for (var cycle = 0; cycle < 2; cycle++)
        {
            sheet.Commands.Undo().Should().BeTrue();
            sheet.Cells.GetMetaData(1, 1, "status").Should().Be("old");
            sheet.Cells.GetMetaData(2, 2, "status").Should().Be(7);
            sheet.Cells.GetMetaData(1, 2, "status").Should().BeNull();
            sheet.Cells.GetMetaData(2, 1, "status").Should().BeNull();

            sheet.Commands.Redo().Should().BeTrue();
            foreach (var position in sheet.Range(region).Positions)
                sheet.Cells.GetMetaData(position.row, position.col, "status").Should().Be(value);

            sheet.Cells.GetMetaData(1, 2, "other").Should().Be("keep");
            sheet.Cells.GetMetaData(0, 0, "status").Should().Be("outside");
            sheet.Cells.GetMetaData(3, 3, "status").Should().BeNull();
        }
    }

    [Test]
    public void Region_MetaData_Events_Report_Each_Cells_Old_And_New_Value_On_Execute_Undo_And_Redo()
    {
        var sheet = new Sheet(2, 3);
        sheet.Cells.SetCellMetaData(1, 1, "status", "old");
        var changes = new List<(int Row, int Col, string Name, object? OldValue, object? NewValue)>();
        sheet.Cells.MetaDataChanged += (_, args) =>
            changes.Add((args.Row, args.Col, args.Name, args.OldValue, args.NewValue));

        sheet.Cells.SetCellMetaData(new Region(1, 1, 1, 2), "status", "ready");
        sheet.Commands.Undo();
        sheet.Commands.Redo();

        changes.Should().Equal(new (int, int, string, object?, object?)[]
        {
            (1, 1, "status", "old", "ready"),
            (1, 2, "status", null, "ready"),
            (1, 1, "status", "ready", "old"),
            (1, 2, "status", "ready", null),
            (1, 1, "status", "old", "ready"),
            (1, 2, "status", null, "ready")
        });
    }

    [TestCase(-1, 1, 0, 1)]
    [TestCase(0, 1, -1, 1)]
    [TestCase(0, 3, 0, 1)]
    [TestCase(0, 1, 0, 3)]
    [TestCase(4, 5, 4, 5)]
    public void Out_Of_Sheet_Region_Is_Rejected_Without_Changes_Events_Or_Undo_Entry(
        int top, int bottom, int left, int right)
    {
        var sheet = new Sheet(3, 3);
        sheet.Cells.SetCellMetaData(0, 0, "status", "old");
        var dirtyCount = 0;
        var changeCount = 0;
        sheet.SheetDirty += (_, _) => dirtyCount++;
        sheet.Cells.MetaDataChanged += (_, _) => changeCount++;

        sheet.Cells.SetCellMetaData(new Region(top, bottom, left, right), "status", "ready")
            .Should().BeFalse();

        dirtyCount.Should().Be(0);
        changeCount.Should().Be(0);
        sheet.Cells.GetMetaData(0, 0, "status").Should().Be("old");
        sheet.Cells.GetMetaData(1, 1, "status").Should().BeNull();
        sheet.Cells.GetMetaData(top, left, "status").Should().NotBe("ready");
        sheet.Commands.Undo().Should().BeTrue();
        sheet.Cells.GetMetaData(0, 0, "status").Should().BeNull();
        sheet.Commands.Undo().Should().BeFalse();
    }

    [Test]
    public void Region_Command_Copies_Its_Target_And_Shares_The_Metadata_Value()
    {
        var sheet = new Sheet(4, 4);
        var region = new Region(0, 1, 0, 1);
        var value = new object();
        var command = new SetRegionMetaDataCommand(region, "data", value);
        region.Shift(2, 2);

        sheet.Commands.ExecuteCommand(command).Should().BeTrue();
        sheet.Cells.GetMetaData(0, 0, "data").Should().BeSameAs(value);
        sheet.Cells.GetMetaData(1, 1, "data").Should().BeSameAs(value);
        sheet.Cells.GetMetaData(2, 2, "data").Should().BeNull();
        sheet.Commands.Undo();
        sheet.Cells.GetMetaData(0, 0, "data").Should().BeNull();
        sheet.Commands.Redo();
        sheet.Cells.GetMetaData(1, 1, "data").Should().BeSameAs(value);
        sheet.Cells.GetMetaData(2, 2, "data").Should().BeNull();
    }

    [Test]
    public void Sheet_Range_Metadata_Clips_To_Sheet_And_Supports_Undo_For_Set_And_Clear_Key()
    {
        var sheet = new Sheet(3, 3);
        var range = sheet.Range(new ColumnRegion(1));
        range.SetMetaData("status", "ready");
        range.Positions.Select(p => sheet.Cells.GetMetaData(p.row, p.col, "status"))
            .Should().Equal("ready", "ready", "ready");
        sheet.Cells.GetMetaData(0, 0, "status").Should().BeNull();
        sheet.Cells.GetMetaData(0, 2, "status").Should().BeNull();

        range.ClearMetaData("status");
        range.Positions.Select(p => sheet.Cells.GetMetaData(p.row, p.col, "status"))
            .Should().OnlyContain(value => value == null);
        sheet.Commands.Undo().Should().BeTrue();
        range.Positions.Select(p => sheet.Cells.GetMetaData(p.row, p.col, "status"))
            .Should().Equal("ready", "ready", "ready");
        sheet.Commands.Undo().Should().BeTrue();
        range.Positions.Select(p => sheet.Cells.GetMetaData(p.row, p.col, "status"))
            .Should().OnlyContain(value => value == null);
        sheet.Commands.Undo().Should().BeFalse();

        sheet.Range(new Region(4, 5, 4, 5)).SetMetaData("status", "outside");
        sheet.Commands.Undo().Should().BeFalse();
        sheet.Cells.GetMetaData(4, 4, "status").Should().BeNull();
    }

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
