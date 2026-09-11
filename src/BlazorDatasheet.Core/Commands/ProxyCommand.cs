using BlazorDatasheet.Core.Protection;
using BlazorDatasheet.Core.Data;

namespace BlazorDatasheet.Core.Commands;

public class ProxyCommand : BaseCommand, IUndoableCommand
{
    private readonly ICommand _command;

    public ProxyCommand(ICommand command)
    {
        _command = command;
    }

    protected override bool ExecuteCore(Sheet sheet)
    {
        return _command.Execute(sheet);
    }

    public override bool CanExecuteProtected(Sheet sheet) => sheet.Protection.CanExecute(_command);

    protected override bool CanExecuteCore(Sheet sheet)
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