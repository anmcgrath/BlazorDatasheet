using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.Events.Layout;

public class HeadingGroupsModifiedEventArgs
{
    public int IndexStart { get; }
    public int IndexEnd { get; }
    public Axis Axis { get; }

    public HeadingGroupsModifiedEventArgs(int indexStart, int indexEnd, Axis axis)
    {
        IndexStart = indexStart;
        IndexEnd = indexEnd;
        Axis = axis;
    }
}
