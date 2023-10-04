using BlazorDatasheet.Data;

namespace BlazorDatasheet.Commands;

public class CommandGroup : IUndoableCommand
{
    private readonly List<ICommand> _commands;
    private readonly List<ICommand> _successfulCommands;

    /// <summary>
    /// Runs a series of commands sequentially, but stops if any fails.
    /// </summary>
    /// <param name="commands"></param>
    public CommandGroup(params ICommand[] commands)
    {
        _commands = commands.ToList();
        _successfulCommands = new List<ICommand>();
    }

    public void AddCommand(ICommand command)
    {
        _commands.Add(command);
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
        foreach (var command in _successfulCommands.Where(cmd => cmd is IUndoableCommand).Cast<IUndoableCommand>())
        {
            undo &= command.Undo(sheet);
        }

        return undo;
    }
}