using System.ComponentModel;
using BlazorDatasheet.Core.Data;

namespace BlazorDatasheet.Core.Events;

/// <summary>
/// Specifies the changes that are to be made to the sheet. If Cancel is set to true, no changes will be made.
/// If any of the CellChanges NewValue's are changed, the values that are set will be affected.
/// </summary>
public class BeforeCellChangeEventArgs : CancelEventArgs
{
    public BeforeCellChangeEventArgs(IEnumerable<CellValueChange> changes)
    {
        Changes = changes;
        Cancel = false;
    }

    /// <summary>
    /// The list of changes to be made to cells. Modify any individual CellChange when desired to affect the changes.
    /// </summary>
    public IEnumerable<CellValueChange> Changes { get; }
}