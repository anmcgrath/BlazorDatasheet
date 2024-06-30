using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.Events.Layout;

public class SizeModifiedEventArgs
{
    public int IndexStart { get; }
    public int IndexEnd { get; }
    public Axis Axis { get; }

    public SizeModifiedEventArgs(int indexStart, int indexEnd, Axis axis)
    {
        IndexStart = indexStart;
        IndexEnd = indexEnd;
        Axis = axis;
    }
}