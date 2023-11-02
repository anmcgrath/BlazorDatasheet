using BlazorDatasheet.Core.Data;

namespace BlazorDatasheet.Core.Commands;

/// <summary>
/// A command that can be un-done/re-run
/// </summary>
public interface IUndoableCommand : ICommand
{
    /// <summary>
    /// Undo the command and return whether it was successful
    /// </summary>
    /// <returns></returns>
    public bool Undo(Sheet sheet);
}