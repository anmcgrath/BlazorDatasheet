using BlazorDatasheet.Core.Data;
using BlazorDatasheet.Core.Protection;

namespace BlazorDatasheet.Core.Commands;

/// <summary>
/// Chainable command.
/// <para>
/// Sheet protection is checked in a single place - <see cref="Execute"/> and <see cref="CanExecute"/> run
/// <see cref="SheetProtection.CanExecute"/> before delegating to <see cref="ExecuteCore"/>/<see cref="CanExecuteCore"/>.
/// Commands declare what they need by overriding <see cref="CanExecuteProtected"/>; they must not repeat the check.
/// </para>
/// </summary>
public abstract class BaseCommand : ICommand, IProtectedCommand
{
    private readonly List<ICommand> _chainedAfterCommands = new();
    private readonly List<ICommand> _chainedBeforeCommands = new();

    /// <summary>
    /// Declares the command's protection requirements. Must be side-effect free.
    /// Commands without a declaration are denied while protection is enforced.
    /// </summary>
    public virtual bool CanExecuteProtected(Sheet sheet) => false;

    public bool Execute(Sheet sheet)
    {
        if (!sheet.Protection.CanExecute(this))
            return false;

        return ExecuteCore(sheet);
    }

    public bool CanExecute(Sheet sheet)
    {
        if (!sheet.Protection.CanExecute(this))
            return false;

        return CanExecuteCore(sheet);
    }

    /// <summary>
    /// Performs the command. Protection has already been checked.
    /// </summary>
    protected abstract bool ExecuteCore(Sheet sheet);

    /// <summary>
    /// Any non-protection pre-conditions for the command. Protection has already been checked.
    /// </summary>
    protected virtual bool CanExecuteCore(Sheet sheet) => true;

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
