using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.DataStructures.Store;

public interface IStore<T,TRestoreData>
{
    /// <summary>
    /// Returns whether the the store contains any non-empty data at the row, column specified.
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// <returns></returns>
    public bool Contains(int row, int col);
    
    /// <summary>
    /// Sets the data at the row, column specified.
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// <param name="value"></param>
    public TRestoreData Set(int row, int col, T value);
    
    /// <summary>
    /// Clears data inside the given region but does not affect the rows/columns arround it.
    /// </summary>
    /// <param name="region"></param>
    /// <returns></returns>
    public TRestoreData Clear(IRegion region);
    
}