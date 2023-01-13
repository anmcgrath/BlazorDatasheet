namespace BlazorDatasheet.Data.Events;

public class SheetInvalidateEventArgs
{
    public IReadOnlySet<(int row, int col)> DirtyCells { get; }

    internal SheetInvalidateEventArgs(IReadOnlySet<(int row, int col)> dirtyCells)
    {
        DirtyCells = dirtyCells;
    }
}