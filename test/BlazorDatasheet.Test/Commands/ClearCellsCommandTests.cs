﻿using BlazorDatasheet.Commands;
using BlazorDatasheet.Data;
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
        _sheet.TrySetCellValue(0, 0, "1");
        _sheet.TrySetCellValue(0, 1, "2");
        _commandManager = new CommandManager(_sheet);
    }

    [Test]
    public void Test_Clear_Then_Undo_Then_Redo()
    {
        _sheet.Selection.SetSingle(new Region(0, 1, 0, 1));
        var cmd = new ClearCellsCommand(_sheet.Selection);
        _commandManager.ExecuteCommand(cmd);
        for (int i = 0; i < 3; i++)
        {
            Assert.Null(_sheet.GetCellValue(0, 0));
            Assert.Null(_sheet.GetCellValue(0, 0));
            _commandManager.Undo();
            Assert.AreEqual("1", _sheet.GetCellValue(0, 0));
            Assert.AreEqual("2", _sheet.GetCellValue(0, 1));
            _commandManager.Redo();
        }
    }
}