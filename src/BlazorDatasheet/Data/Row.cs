using BlazorDatasheet.Interfaces;

namespace BlazorDatasheet.Data;

public class Row
{
    public List<IReadOnlyCell> Cells { get; private set; } = new();
    public int RowNumber { get; internal set; }

    /// <summary>
    /// A row of cells
    /// </summary>
    /// <param name="cells"></param>
    /// <param name="rowNumber"></param>
    public Row(int rowNumber)
    {
        RowNumber = rowNumber;
    }
}