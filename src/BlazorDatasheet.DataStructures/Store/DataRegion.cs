using BlazorDatasheet.DataStructures.Geometry;
using BlazorDatasheet.DataStructures.RTree;
using BlazorDatasheet.DataStructures.Util;

namespace BlazorDatasheet.DataStructures.Store;

public class DataRegion<T> : ISpatialData
{
    public T Data { get; }

    public IRegion Region { get; }
    private Envelope _envelope;

    internal DataRegion(T data, IRegion region)
    {
        Data = data;
        Region = region;
        UpdateEnvelope();
    }

    internal void UpdateEnvelope()
    {
        _envelope = Region.ToEnvelope();
    }

    public ref readonly Envelope Envelope => ref _envelope;
}