namespace BlazorDatasheet.Events;

public class ColumnRemovedEventArgs
{
    public int ColumnIndex { get; }
    public int NCols { get; }

    public ColumnRemovedEventArgs(int columnIndex, int nCols)
    {
        ColumnIndex = columnIndex;
        NCols = nCols;
    }
}