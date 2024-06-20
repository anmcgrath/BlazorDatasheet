using BlazorDatasheet.DataStructures.Search;

namespace BlazorDatasheet.DataStructures.Store;

public class 
    RowDataCollection<T>
{
    public int[] RowIndicies { get; private set; }
    public RowData<T>[] Rows { get; private set; }

    internal RowDataCollection(int[] rowIndicies, RowData<T>[] rows)
    {
        RowIndicies = rowIndicies;
        Rows = rows;
    }

    public RowData<T>? GetRow(int row)
    {
        var index = RowIndicies.BinarySearchIndexOf(row);
        if (index < 0)
            return null;
        return Rows[index];
    }

    public T? GetValue(int row, int col)
    {
        var rowData = GetRow(row);
        if (rowData == null)
            return default(T);

        return rowData.GetColumnData(col);
    }
}