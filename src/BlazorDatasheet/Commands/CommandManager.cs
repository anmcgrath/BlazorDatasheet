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
        if (result)
        {
            if (command is IUndoableCommand undoCommand)
            {
                _history.Push(undoCommand);
            }

            _sheet.Invalidate();
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
        {
            _redos.Push(cmd);
            _sheet.Invalidate();
        }

        return result;
    }

    public bool Redo()
    {
        if (_redos.Peek() == null)
            return false;

        var result = ExecuteCommand(_redos.Pop()!);
        if (result)
            _sheet.Invalidate();
        return result;
    }

    public void ClearHistory()
    {
        _history.Clear();
    }
}