using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Selecting;
using BlazorDatasheet.Core.Util;
using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.Commands;

public class CommandManager
{
    private readonly MaxStack<UndoCommandData> _history;
    private readonly MaxStack<ICommand> _redos;
    private Sheet _sheet;
    private CommandGroup? _currentCommandGroup;

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
    /// <returns></returns>
    public bool ExecuteCommand(ICommand command)
    {
        if (_isCollectingCommands)
        {
            _currentCommandGroup!.AddCommand(command);
            return true;
        }

        var selectionBeforeExecute = _sheet.Selection.Regions.Select(x=>x.Clone()).ToList();
        var result = command.Execute(_sheet);
        if (result)
        {
            if (!HistoryPaused && command is IUndoableCommand undoCommand)
            {
                _history.Push(new UndoCommandData()
                {
                    Command = undoCommand,
                    SelectedRegions = selectionBeforeExecute
                });
            }
        }

        // Clear the redo stack because otherwise we will be redoing changes to the sheet with a changed
        // model from the original time the commands were run.
        _redos.Clear();

        return result;
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

        var cmd = _history.Pop()!;
        var result = cmd.Command.Undo(_sheet);
        
        if (cmd.SelectedRegions.Any())
            _sheet.Selection.Set(cmd.SelectedRegions);

        if (!HistoryPaused && result)
        {
            _redos.Push(cmd.Command);
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

        var cmd = _redos.Pop()!;
        var result = cmd.Execute(_sheet);

        var selectionBeforeExecute = _sheet.Selection.Regions.Select(x=>x.Clone()).ToList();
        if (result)
        {
            if (!HistoryPaused && cmd is IUndoableCommand undoCommand)
            {
                _history.Push(new UndoCommandData()
                {
                    Command = undoCommand,
                    SelectedRegions = selectionBeforeExecute
                });
            }
        }

        return result;
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
    public IUndoableCommand Command { get; init; }

    /// <summary>
    /// The selected range at the time the command was run.
    /// </summary>
    public List<IRegion> SelectedRegions { get; init; }
}