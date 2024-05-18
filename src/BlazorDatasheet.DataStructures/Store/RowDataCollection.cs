namespace BlazorDatasheet.DataStructures.Store;

public class RowDataCollection<T>
{
    public int[] RowIndicies { get; private set; }
    public RowData<T>[] Rows { get; private set; }

    internal RowDataCollection(int[] rowIndicies, RowData<T>[] rows)
    {
        RowIndicies = rowIndicies;
        Rows = rows;
    }
}