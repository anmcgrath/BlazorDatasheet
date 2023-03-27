using System.ComponentModel;
using BlazorDatasheet.Data;

namespace BlazorDatasheet.Events;

public class BeforeCellChangeEventArgs : CancelEventArgs
{
    public BeforeCellChangeEventArgs(IEnumerable<CellChange> changes)
    {
        Changes = changes;
        Cancel = false;
    }

    public IEnumerable<CellChange> Changes { get; }
}