using BlazorDatasheet.Core.Data;

namespace BlazorDatasheet.Core.Commands;

/// <summary>
/// A command that can be executed on the sheet
/// </summary>
public interface ICommand
{
    public bool Execute(Sheet sheet);
    public bool CanExecute(Sheet sheet);

    /// <summary>
    /// Attach a child command, which is executed after the parent command.
    /// </summary>
    /// <param name="command"></param>
    public void AttachAfter(ICommand command);

    /// <summary>
    /// Attach a child command, which is executed before the parent command.
    /// </summary>
    /// <param name="command"></param>
    public void AttachBefore(ICommand command);

    /// <summary>
    /// Get the commands attached to execute after this command.
    /// </summary>
    /// <returns></returns>
    IReadOnlyList<ICommand> GetChainedAfterCommands();

    /// <summary>
    /// Get the commands attached to execute before this command.
    /// </summary>
    /// <returns></returns>
    IReadOnlyList<ICommand> GetChainedBeforeCommands();

    /// <summary>
    /// Clears the commands attached to execute before or after this command.
    /// </summary>
    void ClearChainedCommands();

    void ClearChainedAfterCommands();
    void ClearChainedBeforeCommands();
}