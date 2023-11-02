namespace BlazorDatasheet.DataStructures.Store;

public class MatrixRestoreData<T>
{
    public List<(int row, int col, T? data)> DataRemoved { get; internal set; } = new();
}