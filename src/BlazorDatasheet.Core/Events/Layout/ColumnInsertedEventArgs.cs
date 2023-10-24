namespace BlazorDatasheet.Core.Events.Layout;

public class ColumnInsertedEventArgs
{
    public int ColumnIndex { get; }
    public int NCols { get; }

    public ColumnInsertedEventArgs(int columnIndex, int nCols)
    {
        ColumnIndex = columnIndex;
        NCols = nCols;
    }
}