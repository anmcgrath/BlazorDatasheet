using BlazorDatasheet.Data;
using BlazorDatasheet.Util;

namespace BlazorDatasheet.Commands;

public class CommandManager
{
    private readonly MaxStack<IUndoableCommand> _history;
    private readonly MaxStack<ICommand> _redos;
    private Sheet _sheet;
    private int _maxHistorySize;

    public CommandManager(Sheet sheet, int maxHistorySize = 50)
    {
        _sheet = sheet;
        _maxHistorySize = maxHistorySize;
        _history = new MaxStack<IUndoableCommand>(40);
        _redos = new MaxStack<ICommand>(40);
    }

    public bool ExecuteCommand(ICommand command)
    {
        var result = command.Execute(_sheet);
        if (command is IUndoableCommand undoCommand && result == true)
        {
            _history.Push(undoCommand);
            // Clear redos if there was a successful undoable command
            _redos.Clear();
        }

        return result;
    }

    internal void SetSheet(Sheet sheet)
    {
        _sheet = sheet;
    }

    public bool Undo()
    {
        if (_history.Peek() == null)
            return false;

        var cmd = _history.Pop()!;
        var result = cmd.Undo(_sheet);
        if (result)
            _redos.Push(cmd);
        return result;
    }

    public bool Redo()
    {
        if (_redos.Peek() == null)
            return false;

        return ExecuteCommand(_redos.Pop()!);
    }

    public void ClearHistory()
    {
        _history.Clear();
    }
}