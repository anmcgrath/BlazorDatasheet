using BlazorDatasheet.Data;

namespace BlazorDatasheet.Commands;

public class CommandGroup : IUndoableCommand
{
    private IEnumerable<IUndoableCommand> _commands;

    /// <summary>
    /// Runs a series of commands sequentially, but stops if any fails.
    /// </summary>
    /// <param name="commands"></param>
    public CommandGroup(params IUndoableCommand[] commands)
    {
        _commands = commands;
    }

    public bool Execute(Sheet sheet)
    {
        foreach (var command in _commands)
        {
            var run = command.Execute(sheet);
            if (!run)
                Undo(sheet);
            return false;
        }

        return true;
    }

    public bool Undo(Sheet sheet)
    {
        var undo = true;
        foreach (var command in _commands)
        {
            undo &= command.Undo(sheet);
        }

        return undo;
    }
}