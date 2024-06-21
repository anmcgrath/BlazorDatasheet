using BlazorDatasheet.DataStructures.Geometry;

namespace BlazorDatasheet.DataStructures.Store;

public interface IMatrixDataStore<T> : IStore<T, MatrixRestoreData<T>>
{
    /// <summary>
    /// Returns the data at the row, column specified. If it is empty, returns the default of T.
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// <returns></returns>
    public T? Get(int row, int col);

    /// <summary>
    /// Removes the value at the row/column from the store but does not affect the rows/columns around it.
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    public MatrixRestoreData<T> Clear(int row, int col);

    public MatrixRestoreData<T> Clear(IEnumerable<CellPosition> positions);

    /// <summary>
    /// Clears data inside the specified regions but does not affect the rows/columsn around it.
    /// </summary>
    /// <param name="regions"></param>
    /// <returns></returns>
    public MatrixRestoreData<T> Clear(IEnumerable<IRegion> regions);

    /// <summary>
    /// Finds the next non-empty row number in the column. Returns -1 if no non-empty rows exist after the row
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// <returns>The next non-empty row number in the column. Equals -1 if no non-empty rows exist after the row.</returns>
    public int GetNextNonBlankRow(int row, int col);

    /// <summary>
    /// Finds the next non-empty row number in the column. Returns -1 if no non-empty rows exist after the row
    /// </summary>
    /// <param name="row"></param>
    /// <param name="col"></param>
    /// <returns>The next non-empty row number in the column. Equals -1 if no non-empty rows exist after the row.</returns>
    public int GetNextNonBlankColumn(int row, int col);

    /// <summary>
    /// Removes the rows or columns specified from the store and returns the values that were removed.
    /// </summary>
    /// <param name="index"></param>
    /// <param name="count"></param>
    /// <param name="axis"></param>
    /// <returns></returns>
    public MatrixRestoreData<T> RemoveRowColAt(int index, int count, Axis axis)
    {
        if (axis == Axis.Col)
            return RemoveColAt(index, count);
        else
            return RemoveRowAt(index, count);
    }

    /// <summary>
    /// Get non empty cells that exist in the bounds given
    /// </summary>
    /// <param name="r0">The lower row bound</param>
    /// <param name="r1">The upper row bound</param>
    /// <param name="c0">The lower col bound</param>
    /// <param name="c1">The upper col bound</param>
    /// <returns></returns>
    IEnumerable<CellPosition> GetNonEmptyPositions(int r0, int r1, int c0, int c1);

    /// <summary>
    /// Gets non empty cells that exist in the region given.
    /// </summary>
    /// <param name="region"></param>
    /// <returns></returns>
    IEnumerable<CellPosition> GetNonEmptyPositions(IRegion region) =>
        GetNonEmptyPositions(region.Top, region.Bottom, region.Left, region.Right);

    /// <summary>
    /// Returns the collection of non-empty rows in the region given.
    /// Data in the rows is limited to within the region start and end positions
    /// </summary>
    /// <param name="region"></param>
    /// <returns></returns>
    RowDataCollection<T> GetNonEmptyRowData(IRegion region);

    /// <summary>
    /// Returns the collection of rows in the region given.
    /// Data in the rows is limited to within the region start and end positions
    /// </summary>
    /// <param name="region"></param>
    /// <returns></returns>
    RowDataCollection<T> GetRowData(IRegion region);

    /// <summary>
    /// Copy the data in <paramref name="fromRegion"/> to the position <paramref name="toRegion"/>
    /// </summary>
    /// <param name="fromRegion"></param>
    /// <param name="toRegion"></param>
    /// <returns></returns>
    MatrixRestoreData<T> Copy(IRegion fromRegion, IRegion toRegion);

    void Restore(MatrixRestoreData<T> restoreData);
    public IEnumerable<(int row, int col, T data)> GetNonEmptyData(IRegion region);

    /// <summary>
    /// Returns a 2d array (accessed row, col) of data taken from the region
    /// </summary>
    /// <param name="region"></param>
    /// <returns></returns>
    public T[][] GetData(IRegion region);

    /// <summary>
    /// Returns a sub-store containing only the data in the region specified.
    /// If the <paramref name="newStoreResetsOffsets"/> is true, the new store will have the top-left corner at 0,0.
    /// </summary>
    /// <param name="region">The region to extract data from</param>
    /// <param name="newStoreResetsOffsets">If true, the new store will have the top-left corner at 0,0</param>
    /// <returns></returns>
    IMatrixDataStore<T> GetSubStore(IRegion region, bool newStoreResetsOffsets = true);
}