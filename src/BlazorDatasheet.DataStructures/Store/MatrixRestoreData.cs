namespace BlazorDatasheet.DataStructures.Store;

public class MatrixRestoreData<T>
{
    public List<(int row, int col, T? data)> DataRemoved { get; internal set; } = new();
    public List<AppliedShift>? Shifts { get; internal set; }

    public void Merge(MatrixRestoreData<T> item)
    {
        DataRemoved.AddRange(item.DataRemoved);
        if (item.Shifts != null)
        {
            if (Shifts == null)
                Shifts = item.Shifts;
            else
                Shifts.AddRange(item.Shifts);
        }
    }
}