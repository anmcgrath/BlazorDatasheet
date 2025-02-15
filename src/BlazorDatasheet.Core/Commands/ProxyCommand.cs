using BlazorDatasheet.Core.Data;

namespace BlazorDatasheet.Core.Commands;

public class ProxyCommand : BaseCommand, IUndoableCommand
{
    private readonly ICommand _command;

    public ProxyCommand(ICommand command)
    {
        _command = command;
    }

    public override bool Execute(Sheet sheet)
    {
        return _command.Execute(sheet);
    }

    public override bool CanExecute(Sheet sheet)
    {
        return _command.CanExecute(sheet);
    }

    public bool Undo(Sheet sheet)
    {
        if (_command is IUndoableCommand undo)
            return undo.Undo(sheet);
        return true;
    }
}