namespace BlazorDatasheet.Events;

public class ColumnInsertedEventArgs
{
    public int ColumnIndex { get; }
    public double? Width { get; }
    public int NCols { get; }

    public ColumnInsertedEventArgs(int columnIndex, double? width, int nCols)
    {
        ColumnIndex = columnIndex;
        Width = width;
        NCols = nCols;
    }
}