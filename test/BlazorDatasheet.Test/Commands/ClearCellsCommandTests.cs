using BlazorDatasheet.Commands;
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
        _sheet = new Sheet(1, 2);
        _sheet.TrySetCellValue(0, 0, "1");
        _sheet.TrySetCellValue(0, 1, "2");
        _commandManager = new CommandManager(_sheet);
    }

    [Test]
    public void Test_Clear_Then_Undo_Then_Redo()
    {
        var cmd = new ClearCellsCommand(new Range(0, 0, 0, 1));
        _commandManager.ExecuteCommand(cmd);
        Assert.Null(_sheet.GetCell(0,0).Data);
        Assert.Null(_sheet.GetCell(0,1).Data);
        _commandManager.Undo();
        Assert.AreEqual("1",_sheet.GetCell(0,0).GetValue());
        Assert.AreEqual("2",_sheet.GetCell(0,1).GetValue());
    }
}