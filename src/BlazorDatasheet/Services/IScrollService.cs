using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.Services;

public interface IScrollService
{
    Task ScrollToContainRegion(IRegion region);
}
