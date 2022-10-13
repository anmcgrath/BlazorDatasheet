using BlazorDatasheet.Data;

namespace BlazorDatasheet.Commands;

public interface IUndoableCommand : ICommand
{
    /// <summary>
    /// Undo the command and return whether it was successful
    /// </summary>
    /// <returns></returns>
    public bool Undo(Sheet sheet);
}