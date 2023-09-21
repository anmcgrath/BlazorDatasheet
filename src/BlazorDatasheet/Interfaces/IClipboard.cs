using BlazorDatasheet.Data;
using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Interfaces;

public interface IClipboard
{
    public Task Copy(IRegion region, Sheet sheet);
}