namespace BlazorDatasheet.Model;

public class Row
{
    public List<Cell> Cells { get; private set; }
    public int RowNumber { get; set; }

    public Row(List<Cell> cells, int rowNumber)
    {
        Cells = cells;
        RowNumber = rowNumber;
    }
}