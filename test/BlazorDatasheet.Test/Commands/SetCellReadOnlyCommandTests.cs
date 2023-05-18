using BlazorDatasheet.Commands;
using BlazorDatasheet.Data;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Commands;

public class SetCellReadOnlyCommandTests
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
    public void Test_ReadOnly_Then_Undo_Then_Redo()
    {
       
        var cmd = new SetCellReadOnlyCommand(0,0, true);
        _commandManager.ExecuteCommand(cmd);

        Assert.AreEqual(true, _sheet.GetCell(0,0).IsReadOnly);

        _commandManager.Undo();

        Assert.AreEqual(false, _sheet.GetCell(0, 0).IsReadOnly);
        _commandManager.Redo();

        Assert.AreEqual(true, _sheet.GetCell(0, 0).IsReadOnly);

        _commandManager.ExecuteCommand(cmd);
        Assert.AreEqual(true, _sheet.GetCell(0, 0).IsReadOnly);

        _commandManager.Undo();

        Assert.AreEqual(true, _sheet.GetCell(0, 0).IsReadOnly);

        cmd = new SetCellReadOnlyCommand(0, 0, false);
        _commandManager.ExecuteCommand(cmd);

        Assert.AreEqual(false, _sheet.GetCell(0, 0).IsReadOnly);
    }
}