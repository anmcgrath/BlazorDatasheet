using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.Events.Layout;

public class RowColRemovedEventArgs
{
    /// <summary>
    /// The index where the removal started
    /// </summary>
    public int Index { get; }

    /// <summary>
    /// The total number removed.
    /// </summary>
    public int Count { get; }

    /// <summary>
    /// The axis that the removal occurred on
    /// </summary>
    public Axis Axis { get; }

    public RowColRemovedEventArgs(int index, int count, Axis axis)
    {
        Index = index;
        Count = count;
        Axis = axis;
    }
}