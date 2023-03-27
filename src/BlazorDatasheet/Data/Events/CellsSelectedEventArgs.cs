using BlazorDatasheet.Interfaces;

namespace BlazorDatasheet.Data.Events;

public class CellsSelectedEventArgs
{
    public CellsSelectedEventArgs(IEnumerable<IReadOnlyCell> cells)
    {
        Cells = cells;
    }

    /// <summary>
    /// The cells that are currently selected by the sheet.
    /// </summary>
    public IEnumerable<IReadOnlyCell> Cells { get; }
}