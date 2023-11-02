using System.Collections.Generic;
using System.Linq;
using BlazorDatasheet.Core.Commands;
using BlazorDatasheet.Core.Data;
using FluentAssertions;
using NUnit.Framework;

namespace BlazorDatasheet.Test.Commands;

public class CommandManagerTests
{
    private Sheet _sheet;
    private List<int> _results;

    [SetUp]
    public void Setup()
    {
        _sheet = new Sheet(5, 5);
        _results = new List<int>();
    }

    [Test]
    public void Command_Manager_Max_History_Size()
    {
        var cmdMgr = new CommandManager(_sheet, maxHistorySize: 1);
        cmdMgr.ExecuteCommand(new FakeCommand(0, ref _results));
        cmdMgr.GetUndoCommands().Should().HaveCount(1);
        cmdMgr.ExecuteCommand(new FakeCommand(1, ref _results));
        cmdMgr.GetUndoCommands().Cast<FakeCommand>().Should()
              .ContainSingle(x => x.Id == 1, "because the first command should no longer be in the stack");
    }

    [Test]
    public void When_Command_is_run_Redo_Stack_resets()
    {
        var cmdMgr = new CommandManager(_sheet);
        cmdMgr.ExecuteCommand(new FakeCommand(0, ref _results));
        cmdMgr.GetRedoCommands().Should().HaveCount(0, "because we haven't undone the command yet");
        cmdMgr.Undo();
        cmdMgr.GetRedoCommands().Should().HaveCount(1);
        cmdMgr.ExecuteCommand(new FakeCommand(1, ref _results));
        cmdMgr.GetRedoCommands().Should().HaveCount(0, "because we executed a command");
    }

    [Test]
    public void Pause_History_Doesnt_Add_To_Undo_Stack()
    {
        var cmdMgr = new CommandManager(_sheet);
        cmdMgr.ExecuteCommand(new FakeCommand(0, ref _results));
        cmdMgr.ExecuteCommand(new FakeCommand(1, ref _results));
        cmdMgr.PauseHistory();
        cmdMgr.ExecuteCommand(new FakeCommand(2, ref _results));
        cmdMgr.ResumeHistory();
        _results.Should().Equal(new[] { 0, 1, 2 });
        cmdMgr.GetUndoCommands().Should().HaveCount(2);
    }

    [Test]
    public void Collect_Commands_In_Group_And_Execute()
    {
        var cmdMgr = new CommandManager(_sheet);
        cmdMgr.BeginCommandGroup();
        cmdMgr.ExecuteCommand(new FakeCommand(0, ref _results));
        cmdMgr.ExecuteCommand(new FakeCommand(1, ref _results));
        _results.Should().BeEmpty("because we haven't ended the command group yet");
        cmdMgr.EndCommandGroup();
        _results.Should().Equal(new[] { 0, 1 }, "because we should now execute commands in order");
        cmdMgr.Undo();
        _results.Should().BeEmpty();
    }
}

public class FakeCommand : IUndoableCommand
{
    public int Id { get; }
    private List<int> _cmdExecutions;

    public FakeCommand(int id, ref List<int> cmdExecutions)
    {
        Id = id;
        _cmdExecutions = cmdExecutions;
    }

    public bool Execute(Sheet sheet)
    {
        _cmdExecutions.Add(Id);
        return true;
    }

    public bool Undo(Sheet sheet)
    {
        _cmdExecutions.RemoveAt(_cmdExecutions.Count - 1);
        return true;
    }
}