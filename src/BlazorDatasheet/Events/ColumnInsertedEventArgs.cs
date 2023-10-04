namespace BlazorDatasheet.Events;

public class ColumnInsertedEventArgs
{
    public int ColumnIndex { get; }
    public double? Width { get; }

    public ColumnInsertedEventArgs(int columnIndex, double? width)
    {
        ColumnIndex = columnIndex;
        Width = width;
    }
}