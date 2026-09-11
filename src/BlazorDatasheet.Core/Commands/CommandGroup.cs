using BlazorDatasheet.Core.Protection;
using BlazorDatasheet.Core.Data;

namespace BlazorDatasheet.Core.Commands;

public class CommandGroup : BaseCommand, IUndoableCommand
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

    public CommandGroup(List<ICommand> commands)
    {
        _commands = commands;
        _successfulCommands = new List<ICommand>();
    }

    public void AddCommand(ICommand command)
    {
        _commands.Add(command);
    }

    public override bool CanExecuteProtected(Sheet sheet) => _commands.All(sheet.Protection.CanExecute);

    protected override bool CanExecuteCore(Sheet sheet)
    {
        return _commands.All(x => x.CanExecute(sheet));
    }

    protected override bool ExecuteCore(Sheet sheet)
    {
        _successfulCommands.Clear();

        using var updates = sheet.SuspendUpdates();
        foreach (var command in _commands)
        {
            var run = sheet.Commands.ExecuteCommand(command, isRedo: false, useUndo: false);
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
        using var updates = sheet.SuspendUpdates();
        var undo = true;
        var undoCommands =
            _successfulCommands
                .Where(cmd => cmd is IUndoableCommand).Cast<IUndoableCommand>().ToList();

        undoCommands.Reverse();
        foreach (var command in undoCommands)
        {
            undo &= command.Undo(sheet);
        }

        return undo;
    }
}