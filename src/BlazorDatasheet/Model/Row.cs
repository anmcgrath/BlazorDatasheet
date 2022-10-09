namespace BlazorDatasheet.Model;

public class Row
{
    public List<Cell> Cells { get; private set; }
    public int RowNumber { get; private set; }

    /// <summary>
    /// A row of cells
    /// </summary>
    /// <param name="cells"></param>
    /// <param name="rowNumber"></param>
    public Row(List<Cell> cells, int rowNumber)
    {
        Cells = cells;
        RowNumber = rowNumber;
    }
}