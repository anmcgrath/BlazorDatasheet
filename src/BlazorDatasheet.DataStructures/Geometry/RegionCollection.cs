namespace BlazorDatasheet.DataStructures.Geometry;

/// <summary>
/// Holds a collection of regions. The regions are stored in an RTree for efficient positional lookup.
/// </summary>
public class RegionCollection<T>
{
    /// <summary>
    /// Adds a region to the collection.
    /// </summary>
    /// <param name="region"></param>
    /// <exception cref="NotImplementedException"></exception>
    void Add(IRegion region)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Cuts the region by removing parts of the region intersecting with any regions.
    /// </summary>
    /// <param name="region"></param>
    void Cut(IRegion region)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Returns all regions intersecting with the region given
    /// </summary>
    /// <param name="region"></param>
    /// <returns></returns>
    IRegion[] GetIntersecting(IRegion region)
    {
        throw new NotImplementedException();
    }
}