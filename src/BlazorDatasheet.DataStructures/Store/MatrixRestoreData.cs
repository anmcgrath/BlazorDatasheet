using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.DataStructures.Store;

public class MatrixRestoreData<T>
{
    public List<(int row, int col, T? data)> DataRemoved { get; internal set; } = new();
    public List<CellData<T>> PositionsSet { get; internal set; } = new();
    public List<AppliedShift> Shifts { get; internal set; } = new();

    public void Merge(MatrixRestoreData<T> item)
    {
        DataRemoved.AddRange(item.DataRemoved);
        PositionsSet.AddRange(item.PositionsSet);
        Shifts.AddRange(item.Shifts);
    }
}