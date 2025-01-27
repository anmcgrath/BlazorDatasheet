﻿using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Events.Commands;
using BlazorDatasheet.Core.Selecting;
using BlazorDatasheet.Core.Util;

namespace BlazorDatasheet.Core.Commands;

public class CommandManager
{
    private readonly MaxStack<UndoCommandData> _history;
    private readonly MaxStack<ICommand> _redos;
    private readonly Sheet _sheet;
    private CommandGroup? _currentCommandGroup;

    /// <summary>
    /// Invoked before a command is run. If cancel is true in <see cref="BeforeCommandRunEventArgs"/>, the command is not run.
    /// </summary>
    public event EventHandler<BeforeCommandRunEventArgs>? BeforeCommandRun;

    /// <summary>
    /// Called after a command is run.
    /// </summary>
    public event EventHandler<CommandRunEventArgs>? CommandRun;

    /// <summary>
    /// Called after a command was undone.
    /// </summary>
    public event EventHandler<UndoCommandRunEventArgs>? CommandUndone;

    /// <summary>
    /// Whether the commands executed are being collected in a group.
    /// </summary>
    private bool _isCollectingCommands;

    /// <summary>
    /// If history is paused, commands are no longer added to the undo/redo stack.
    /// </summary>
    public bool HistoryPaused { get; private set; }

    public CommandManager(Sheet sheet, int maxHistorySize = 50)
    {
        _sheet = sheet;
        _history = new MaxStack<UndoCommandData>(maxHistorySize);
        _redos = new MaxStack<ICommand>(maxHistorySize);
    }

    /// <summary>
    /// Executes a command and, if it is an undoable command, adds it to the undo stack.
    /// </summary>
    /// <param name="command"></param>
    /// <param name="isRedo"></param>
    /// <returns></returns>
    public bool ExecuteCommand(ICommand command, bool isRedo = false)
    {
        if (_isCollectingCommands)
        {
            _currentCommandGroup!.AddCommand(command);
            return true;
        }

        var beforeArgs = new BeforeCommandRunEventArgs(command, _sheet);
        BeforeCommandRun?.Invoke(this, beforeArgs);

        if (beforeArgs.Cancel)
            return false;

        var chainedBeforeResult = ExecuteCommands(command.GetChainedBeforeCommands());
        if (!chainedBeforeResult)
            return false;

        if (!command.CanExecute(_sheet))
            return false;

        var result = command.Execute(_sheet);
        CommandRun?.Invoke(this, new CommandRunEventArgs(command, _sheet, result));

        var chainedAfterResult = ExecuteCommands(command.GetChainedAfterCommands(), isRedo: isRedo);
        if (!chainedAfterResult && command is IUndoableCommand undoableCommand)
        {
            undoableCommand.Undo(_sheet);
            return false;
        }

        if (result)
        {
            if (!HistoryPaused && command is IUndoableCommand undoCommand)
            {
                _history.Push(new UndoCommandData()
                {
                    Command = undoCommand,
                    SelectionSnapshot = _sheet.Selection.GetSelectionSnapshot()
                });
            }
        }

        // Clear the redo stack because otherwise we will be redoing changes to the sheet with a changed
        // model from the original time the commands were run.
        if (!isRedo)
            _redos.Clear();

        return result;
    }

    /// <summary>
    /// Executes multiple commands and returns true if all are successful.
    /// Rolls back any unsuccessful undoable commands so that if one fails to run, all fail to run.
    /// </summary>
    /// <param name="commands"></param>
    /// <param name="isRedo"></param>
    /// <returns></returns>
    private bool ExecuteCommands(IReadOnlyList<ICommand> commands, bool isRedo = false)
    {
        if (!commands.Any())
            return true;

        int lastSuccessfulCommandIndex = -1;
        for (int i = 0; i < commands.Count; i++)
        {
            var chainedCommand = commands[i];
            var res = ExecuteCommand(chainedCommand, isRedo);
            if (res)
                lastSuccessfulCommandIndex = i;
            else
                break;
        }

        var anyCommandsFailed = lastSuccessfulCommandIndex != commands.Count - 1;
        if (anyCommandsFailed)
        {
            for (int i = lastSuccessfulCommandIndex; i >= 0; i--)
            {
                if (commands[i] is IUndoableCommand undoCommand)
                {
                    undoCommand.Undo(_sheet);
                    undoCommand.ClearChainedCommands();
                }
            }
        }

        return !anyCommandsFailed;
    }

    /// <summary>
    /// Returns all the undo commands in the undo stack
    /// </summary>
    /// <returns></returns>
    public IEnumerable<ICommand> GetUndoCommands()
    {
        return _history.GetAllItems().Select(x => x.Command);
    }

    /// <summary>
    /// Returns all the redo commands in the redo stack.
    /// </summary>
    /// <returns></returns>
    public IEnumerable<ICommand> GetRedoCommands()
    {
        return _redos.GetAllItems();
    }

    /// <summary>
    /// Executes the undo function of the last undo-able command run.
    /// </summary>
    /// <returns></returns>
    public bool Undo()
    {
        if (_isCollectingCommands)
            return false;

        if (_history.Peek() == null)
            return false;

        var commandData = _history.Pop()!;

        foreach (var cmd in commandData.Command.GetChainedAfterCommands().Reverse())
        {
            if (cmd is IUndoableCommand undoableCommand)
            {
                var afterRes = undoableCommand.Undo(_sheet);
                undoableCommand.ClearChainedCommands();
                CommandUndone?.Invoke(this, new UndoCommandRunEventArgs(undoableCommand, _sheet, afterRes));
            }
        }

        var result = commandData.Command.Undo(_sheet);
        CommandUndone?.Invoke(this, new UndoCommandRunEventArgs(commandData.Command, _sheet, result));

        foreach (var cmd in commandData.Command.GetChainedBeforeCommands().Reverse())
        {
            if (cmd is IUndoableCommand undoableCommand)
            {
                var beforeRes = undoableCommand.Undo(_sheet);
                undoableCommand.ClearChainedCommands();
                CommandUndone?.Invoke(this, new UndoCommandRunEventArgs(undoableCommand, _sheet, beforeRes));
            }
        }

        commandData.Command.ClearChainedCommands();
        _sheet.Selection.Restore(commandData.SelectionSnapshot);

        if (!HistoryPaused && result)
        {
            _redos.Push(commandData.Command);
        }

        return result;
    }

    /// <summary>
    /// Execute the last command that was un-done.
    /// </summary>
    /// <returns></returns>
    public bool Redo()
    {
        if (_isCollectingCommands)
            return false;

        if (_redos.Peek() == null)
            return false;

        var command = _redos.Pop()!;
        return ExecuteCommand(command, isRedo: true);
    }

    /// <summary>
    /// Removes all commands from the history, clearing the undo/redo functionality for those commands.
    /// </summary>
    public void ClearHistory()
    {
        _history.Clear();
    }

    /// <summary>
    /// Stop commands to be added to the undo/redo stack.
    /// </summary>
    public void PauseHistory()
    {
        HistoryPaused = true;
    }

    /// <summary>
    /// Allow commands to be added to the undo/redo stack.
    /// </summary>
    public void ResumeHistory()
    {
        HistoryPaused = false;
    }

    /// <summary>
    /// Starts collecting commands in a group. When collecting is finished, via EndCommandGroup()
    /// the commands are executed. When the commands are then undone/redone, they are undone/redone together.
    /// </summary>
    public void BeginCommandGroup()
    {
        _isCollectingCommands = true;
        _currentCommandGroup = new CommandGroup();
    }

    /// <summary>
    /// Finishes collecting commands in a group and executes the commands in the group.
    /// </summary>
    public bool EndCommandGroup()
    {
        _isCollectingCommands = false;
        if (_currentCommandGroup != null)
        {
            var res = this.ExecuteCommand(_currentCommandGroup);
            _currentCommandGroup = null;
            return res;
        }

        _currentCommandGroup = null;
        return false;
    }
}

internal class UndoCommandData
{
    /// <summary>
    /// The command to undo.
    /// </summary>
    public IUndoableCommand Command { get; init; } = null!;

    /// <summary>
    /// Records information on the sheet's selection so that it can be restored.
    /// </summary>
    public required SelectionSnapshot SelectionSnapshot { get; init; }
}