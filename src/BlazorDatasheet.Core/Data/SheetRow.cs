namespace BlazorDatasheet.Core.Data;

public class SheetRow
{
    public int Row { get; }
    public int RowIndex { get; }

    public SheetRow(int rowIndex)
    {
        RowIndex = rowIndex;
        Row = rowIndex + 1;
    }
}