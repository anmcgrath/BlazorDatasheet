namespace BlazorDatasheet.Data.Events;

public class CellsChangedEventArgs
{
    public IEnumerable<CellChangedInfo> CellsChanged { get; }

    public CellsChangedEventArgs(IEnumerable<CellChangedInfo> cellsChanged)
    {
        CellsChanged = cellsChanged;
    }
    
    /// <summary>
    /// Create a cells changed event for a single cell
    /// </summary>
    /// <param name="cell"></param>
    /// <param name="row"></param>
    /// <param name="col"></param>
    public CellsChangedEventArgs(Cell cell, int row, int col)
    {
        CellsChanged = new List<CellChangedInfo>()
        {
            new CellChangedInfo(cell, row, col)
        };
    }
}