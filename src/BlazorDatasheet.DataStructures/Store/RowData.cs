namespace BlazorDatasheet.DataStructures.Store;

public class RowData<T>
{
    public int Row { get; private set; }
    public int[] ColumnIndices { get; set; }
    public T[] Values { get; set; }

    public RowData(int row, int[] columnIndices, T[] values)
    {
        ColumnIndices = columnIndices;
        Values = values;
        Row = row;
    }

    public T? GetColumnData(int columnIndex)
    {
        var index = Array.IndexOf(ColumnIndices, columnIndex);
        if (index < 0)
            return default(T);
        
        return Values[index];
    }
}