using System.Collections.Generic;
using BlazorDatasheet.Core.Commands;
using BlazorDatasheet.Core.Commands.Data;
using BlazorDatasheet.Core.Data;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Commands;

public class ChangeCellValueCommandTests
{
    private Sheet _sheet;
    private CommandManager _commandManager;

    [SetUp]
    public void Setup()
    {
        //Create a sheet with only one cell, with a value 1
        _sheet = new Sheet(1, 1);
        _sheet.Cells.SetValue(0, 0, 1);
        _commandManager = new CommandManager(_sheet);
    }

    [Test]
    public void Execute_Change_Cell_Command_Correctly_Changes_Value_On_Sheet()
    {
        var changeCmd = new SetCellValueCommand(0, 0, 10);
        _commandManager.ExecuteCommand(changeCmd);
        Assert.AreEqual(10, _sheet.Cells.GetCell(0, 0).GetValue<int>());
        _commandManager.Undo();
        Assert.AreEqual(1, _sheet.Cells.GetCell(0, 0).GetValue<int>());
        //Try to undo again but nothing changed because there's no more undo commands
        _commandManager.Undo();
        Assert.AreEqual(1, _sheet.Cells.GetCell(0, 0).GetValue<int>());
    }
}