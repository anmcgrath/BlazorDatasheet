using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.Events.Layout;

public class RowColInsertedEventArgs
{
    /// <summary>
    /// The index where the insertion started
    /// </summary>
    public int Index { get; }

    /// <summary>
    /// The number inserted
    /// </summary>
    public int Count { get; }

    /// <summary>
    /// The axis that the insertion occurred on
    /// </summary>
    public Axis Axis { get; }

    public RowColInsertedEventArgs(int index, int count, Axis axis)
    {
        Index = index;
        Count = count;
        Axis = axis;
    }
}