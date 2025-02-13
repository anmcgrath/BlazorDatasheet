using BlazorDatasheet.Core.Data;

namespace BlazorDatasheet.Core.Commands;

public abstract class BaseCommand : ICommand
{
    private readonly List<ICommand> _chainedAfterCommands = new();
    private readonly List<ICommand> _chainedBeforeCommands = new();
    public abstract bool Execute(Sheet sheet);
    public abstract bool CanExecute(Sheet sheet);

    public void AttachAfter(ICommand command)
    {
        _chainedAfterCommands.Add(command);
    }

    public void AttachBefore(ICommand command)
    {
        _chainedBeforeCommands.Add(command);
    }

    public virtual IReadOnlyList<ICommand> GetChainedAfterCommands() => _chainedAfterCommands;
    public virtual IReadOnlyList<ICommand> GetChainedBeforeCommands() => _chainedBeforeCommands;

    public void ClearChainedCommands()
    {
        _chainedAfterCommands.Clear();
        _chainedBeforeCommands.Clear();
    }

    public void ClearChainedBeforeCommands()
    {
        _chainedBeforeCommands.Clear();
    }

    public void ClearChainedAfterCommands()
    {
        _chainedAfterCommands.Clear();
    }
}