using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.Events.Layout;

public class HeadingsModifiedEventArgs
{
    public int IndexStart { get; }
    public int IndexEnd { get; }
    public Axis Axis { get; }

    public HeadingsModifiedEventArgs(int indexStart, int indexEnd, Axis axis)
    {
        IndexStart = indexStart;
        IndexEnd = indexEnd;
        Axis = axis;
    }
}
