using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.Events.Layout;

public class SizeModifiedEventArgs
{
    public int IndexStart { get; }
    public double IndexEnd { get; }
    public Axis Axis { get; }

    public SizeModifiedEventArgs(int indexStart, double indexEnd, Axis axis)
    {
        IndexStart = indexStart;
        IndexEnd = indexEnd;
        Axis = axis;
    }
}