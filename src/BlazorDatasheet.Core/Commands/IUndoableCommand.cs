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

    /// <summary>
    /// Roughly how many cell values the command keeps so that it can be undone or redone. The
    /// command manager drops the oldest history entries when the total exceeds
    /// <see cref="CommandManager.MaxHistoryCells"/>. Commands that keep little or nothing leave this at zero.
    /// </summary>
    public int RetainedCellCount => 0;
}