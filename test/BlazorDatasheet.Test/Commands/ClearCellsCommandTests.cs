using BlazorDatasheet.Core.Commands;
using BlazorDatasheet.Core.Commands.Data;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Formats;
using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.Formula.Core;
using FluentAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Commands;

public class ClearCellsCommandTests
{
    private Sheet _sheet;
    private CommandManager _commandManager;

    [SetUp]
    public void Setup()
    {
        //Create a sheet with only one cell, with a value 1
        _sheet = new Sheet(2, 2);
        _sheet.Cells.SetValue(0, 0, "1");
        _sheet.Cells.SetValue(0, 1, "2");
        _commandManager = new CommandManager(_sheet);
    }

    [Test]
    public void Test_Clear_Then_Undo_Then_Redo()
    {
        _sheet.Selection.Set(new Region(0, 1, 0, 1));
        var cmd = new ClearCellsCommand(_sheet.Selection.Regions);
        _commandManager.ExecuteCommand(cmd);
        for (int i = 0; i < 3; i++)
        {
            Assert.Null(_sheet.Cells.GetValue(0, 0));
            Assert.Null(_sheet.Cells.GetValue(0, 0));
            _sheet.Cells.GetCellValue(0, 0).ValueType.Should().Be(CellValueType.Empty);
            _commandManager.Undo();
            Assert.AreEqual(1, _sheet.Cells.GetValue(0, 0));
            Assert.AreEqual(2, _sheet.Cells.GetValue(0, 1));
            _commandManager.Redo();
        }
    }

    [Test]
    public void Clear_Command_With_Readonly_Cells_Cannot_Execute()
    {
        _sheet.Cells[1, 1].Value = "Not Cleared";
        _sheet.Cells[1, 1].Format = new CellFormat()
        {
            IsReadOnly = true
        };
        _sheet.Range("A1:C10")!.Clear();
        _sheet.Cells[1, 1].Value.Should().Be("Not Cleared");
    }
}