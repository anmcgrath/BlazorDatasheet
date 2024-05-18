namespace BlazorDatasheet.DataStructures.Store;

public class RowData<T>
{
    private int[] ColumnIndices { get; set; }
    private T[] Values { get; set; }

    public RowData(int[] columnIndices, T[] values)
    {
        ColumnIndices = columnIndices;
        Values = values;
    }

    public T? GetColumnData(int columnIndex)
    {
        var index = Array.IndexOf(ColumnIndices, columnIndex);
        if (index < 0)
            return default(T);
        
        return Values[index];
    }
}