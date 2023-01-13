using BlazorDatasheet.Data;

namespace BlazorDatasheet.Commands;

public class CommandGroup : IUndoableCommand
{
    private IEnumerable<IUndoableCommand> _commands;
    private List<IUndoableCommand> _successfulCommands;

    /// <summary>
    /// Runs a series of commands sequentially, but stops if any fails.
    /// </summary>
    /// <param name="commands"></param>
    public CommandGroup(params IUndoableCommand[] commands)
    {
        _commands = commands;
        _successfulCommands = new List<IUndoableCommand>();
    }

    public bool Execute(Sheet sheet)
    {
        _successfulCommands.Clear();

        foreach (var command in _commands)
        {
            var run = command.Execute(sheet);
            if (!run)
            {
                // Undo any successful commands that have been run
                Undo(sheet);
                return false;
            }
            else
                _successfulCommands.Add(command);
        }

        return true;
    }

    public bool Undo(Sheet sheet)
    {
        var undo = true;
        foreach (var command in _successfulCommands)
        {
            undo &= command.Undo(sheet);
        }

        return undo;
    }
}