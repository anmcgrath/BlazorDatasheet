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
    
    /// <summary>
    /// Inserts <paramref name="count"/> rows or columns, depending on the <paramref name="axis"/>
    /// </summary>
    /// <param name="index"></param>
    /// <param name="count"></param>
    /// <param name="axis"></param>
    /// <returns></returns>
    public TRestoreData InsertRowColAt(int index, int count, Axis axis)
    {
        if (axis == Axis.Col)
            return InsertColAt(index, count);
        else
            return InsertRowAt(index, count);
    }
    
    /// <summary>
    /// Inserts a row into the store
    /// </summary>
    /// <param name="row">The index of the row that the new row will now be.</param>
    /// <param name="nRows">The number of rows to inser</param>
    public TRestoreData InsertRowAt(int row, int nRows);

    /// <summary>
    /// Inserts a column into the store
    /// </summary>
    /// <param name="col">The index of the column that the new column is inserted AFTER</param>
    public TRestoreData InsertColAt(int col, int nCols);
    
}