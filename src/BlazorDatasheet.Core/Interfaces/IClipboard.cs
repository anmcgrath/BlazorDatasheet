using BlazorDatasheet.Core.Data;
using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Core.Interfaces;

public interface IClipboard
{
    public Task Copy(IRegion region, Sheet sheet);
}