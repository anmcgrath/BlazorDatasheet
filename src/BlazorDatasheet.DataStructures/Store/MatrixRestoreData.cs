namespace BlazorDatasheet.DataStructures.Store;

public class MatrixRestoreData<T>
{
    private List<(int row, int col, T? data)>? _dataRemoved;
    
    public List<(int row, int col, T? data)> DataRemoved
    {
        get => _dataRemoved ??= new List<(int row, int col, T? data)>();
        internal set => _dataRemoved = value;
    }

    public List<AppliedShift>? Shifts { get; internal set; }

    public void Merge(MatrixRestoreData<T> item)
    {
        if (item._dataRemoved is { Count: > 0 })
            DataRemoved.AddRange(item._dataRemoved);
        if (item.Shifts != null)
        {
            if (Shifts == null)
                Shifts = item.Shifts;
            else
                Shifts.AddRange(item.Shifts);
        }
    }
}