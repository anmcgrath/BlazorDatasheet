using System;
using System.Collections.Generic;
using System.Linq;
using BlazorDatasheet.Core.Commands;
using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Events.Commands;
using BlazorDatasheet.DataStructures.Geometry;
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

    [Test]
    public void Undo_Command_Sets_Selection_To_Original_Value()
    {
        var cmd = new FakeCommand(0, ref _results);
        _sheet.Selection.Set(new Region(0, 10, 0, 10));
        _sheet.Selection.MoveActivePositionByCol(1);
        _sheet.Selection.MoveActivePositionByRow(1); // set activeposition to (1,1)
        _sheet.Commands.ExecuteCommand(cmd);
        _sheet.Commands.Undo();
        _sheet.Selection.ActiveRegion.Should().BeEquivalentTo(new Region(0, 10, 0, 10));
        _sheet.Selection.ActiveCellPosition.Should().BeEquivalentTo(new CellPosition(1, 1));
    }

    [Test]
    public void Command_Run_Fires_Event()
    {
        var cmdRun = false;
        var cmd = new FakeCommand(0, ref _results);
        _sheet.Commands.CommandRun += (sender, args) => cmdRun = true;
        _sheet.Commands.ExecuteCommand(cmd);
        cmdRun.Should().Be(true);
    }

    [Test]
    public void Command_Undone_Fires_Event()
    {
        var cmdUndone = false;
        var isSameCmd = false;
        var cmd = new FakeCommand(0, ref _results);
        _sheet.Commands.CommandUndone += (sender, args) =>
        {
            cmdUndone = true;
            isSameCmd = args.Command == cmd;
        };
        _sheet.Commands.ExecuteCommand(cmd);
        _sheet.Commands.Undo();
        cmdUndone.Should().Be(true);
        isSameCmd.Should().Be(true);
    }

    [Test]
    public void Command_That_Cannot_Execute_Doesnt_Execute_Command()
    {
        var cmd = new FakeCommand(0, ref _results, canExecute: false);
        _sheet.Commands.ExecuteCommand(cmd);
        _results.Should().BeEmpty();
    }


    [Test]
    public void Attach_Command_Will_Execute_Command_After()
    {
        _sheet.Commands.CommandRun += (sender, args) =>
        {
            if (args.Command is FakeCommand command)
            {
                // use a proxy command so we don't end up with a stack overflow exception.
                command.AttachAfter(new ProxyCommand(new FakeCommand(-1, ref _results)));
            }
        };

        _sheet.Commands.ExecuteCommand(new FakeCommand(0, ref _results));
        _results.Should().BeEquivalentTo([0, -1]);
    }

    [Test]
    public void Attach_Command_Will_Execute_Command_Before()
    {
        _sheet.Commands.BeforeCommandRun += (sender, args) =>
        {
            if (args.Command is FakeCommand command)
            {
                // use a proxy command so we don't end up with a stack overflow exception.
                command.AttachBefore(new ProxyCommand(new FakeCommand(-1, ref _results)));
            }
        };

        _sheet.Commands.ExecuteCommand(new FakeCommand(0, ref _results));
        _results.Should().BeEquivalentTo([-1, 0]);
    }

    [Test]
    public void Failed_Attached_Command_Will_Undo_All_Commands()
    {
        _sheet.Commands.CommandRun += (sender, args) =>
        {
            if (args.Command is FakeCommand command)
            {
                // use a proxy command so we don't end up with a stack overflow exception.
                command.AttachAfter(new ProxyCommand(new FakeCommand(-1, ref _results)));
                command.AttachAfter(new ProxyCommand(new FakeCommand(-2, ref _results, false)));
            }
        };

        _sheet.Commands.ExecuteCommand(new FakeCommand(0, ref _results));
        _results.Should().BeEmpty();
    }

    [Test]
    public void Undo_Wil_Undo_All_Attached_After()
    {
        _sheet.Commands.CommandRun += (sender, args) =>
        {
            if (args.Command is FakeCommand command)
            {
                // use a proxy command so we don't end up with a stack overflow exception.
                command.AttachAfter(new ProxyCommand(new FakeCommand(2, ref _results)));
                command.AttachAfter(new ProxyCommand(new FakeCommand(3, ref _results)));
            }
        };

        // add a command first to ensure we don't undo this far back.
        _sheet.Commands.ExecuteCommand(new ProxyCommand(new FakeCommand(0, ref _results)));

        _sheet.Commands.ExecuteCommand(new FakeCommand(1, ref _results));
        _sheet.Commands.Undo();
        _results.Should().BeEquivalentTo([0]);

        _sheet.Commands.Redo();
        _results.Should().BeEquivalentTo([0, 1, 2, 3]);
    }

    [Test]
    public void Undo_Wil_Undo_All_Attached_Before()
    {
        _sheet.Commands.BeforeCommandRun += (sender, args) =>
        {
            if (args.Command is FakeCommand command)
            {
                // use a proxy command so we don't end up with a stack overflow exception.
                command.AttachBefore(new ProxyCommand(new FakeCommand(2, ref _results)));
                command.AttachBefore(new ProxyCommand(new FakeCommand(3, ref _results)));
            }
        };

        // add a command first to ensure we don't undo this far back.
        _sheet.Commands.ExecuteCommand(new ProxyCommand(new FakeCommand(0, ref _results)));
        _sheet.Commands.ExecuteCommand(new FakeCommand(1, ref _results));

        _sheet.Commands.Undo();
        _results.Should().BeEquivalentTo([0]);

        _sheet.Commands.Redo();
        _results.Should().BeEquivalentTo([0, 2, 3, 1]);
    }

    [Test]
    public void New_Command_Run_From_A_Handler_During_Redo_Clears_The_Redo_Stack()
    {
        _sheet.Cells.SetValue(0, 0, 1);
        _sheet.Cells.SetValue(1, 0, 2);
        _sheet.Commands.Undo();
        _sheet.Commands.Undo();
        _sheet.Commands.GetRedoCommands().Should().HaveCount(2);

        EventHandler<CommandRunEventArgs>? handler = null;
        handler = (_, _) =>
        {
            _sheet.Commands.CommandRun -= handler;
            _sheet.Cells.SetValue(4, 4, "a new change");
        };
        _sheet.Commands.CommandRun += handler;

        _sheet.Commands.Redo();
        // the change made from the handler invalidates anything left on the redo stack
        _sheet.Commands.GetRedoCommands().Should().BeEmpty();
    }

    [Test]
    public void Redo_That_Cannot_Run_Is_Removed_From_The_Redo_Stack()
    {
        _sheet.Cells.SetValue(0, 0, 1);
        _sheet.Commands.Undo();
        _sheet.Commands.GetRedoCommands().Should().HaveCount(1);

        void Cancel(object? sender, BeforeCommandRunEventArgs args) => args.Cancel = true;
        _sheet.Commands.BeforeCommandRun += Cancel;
        _sheet.Commands.Redo().Should().BeFalse();
        _sheet.Commands.BeforeCommandRun -= Cancel;

        _sheet.Commands.GetRedoCommands().Should().BeEmpty();
        _sheet.Commands.Redo().Should().BeFalse();
    }

    [Test]
    public void Command_Not_Executed_Fires_Event()
    {
        var notExecutedCount = 0;
        _sheet.Commands.CommandNotExecuted += (sender, args) => notExecutedCount++;
        _sheet.Commands.ExecuteCommand(new FakeCommand(0, ref _results, false));
        notExecutedCount.Should().Be(1);
    }
}

public class FakeCommand : BaseCommand, IUndoableCommand
{
    public int Id { get; }
    private List<int> _cmdExecutions;
    private readonly bool _canExecute;

    public FakeCommand(int id, ref List<int> cmdExecutions, bool canExecute = true)
    {
        Id = id;
        _cmdExecutions = cmdExecutions;
        _canExecute = canExecute;
    }

    protected override bool ExecuteCore(Sheet sheet)
    {
        _cmdExecutions.Add(Id);
        return true;
    }

    protected override bool CanExecuteCore(Sheet sheet) => _canExecute;

    public bool Undo(Sheet sheet)
    {
        _cmdExecutions.RemoveAt(_cmdExecutions.Count - 1);
        return true;
    }
}